using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Presentation.DTOs;
using TelegramPhotoBot.Presentation.Handlers;

namespace TelegramPhotoBot.Presentation.Services;

/// <summary>
/// Background service that polls Telegram for updates
/// </summary>
public class TelegramBotPollingService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramBotPollingService> _logger;

    public TelegramBotPollingService(
        ITelegramBotClient botClient,
        IServiceProvider serviceProvider,
        ILogger<TelegramBotPollingService> logger)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram bot polling...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] 
            { 
                UpdateType.Message, 
                UpdateType.CallbackQuery, 
                UpdateType.PreCheckoutQuery,
                UpdateType.MessageReaction  // Enable reaction updates
            }
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        var me = await _botClient.GetMeAsync(stoppingToken);
        _logger.LogInformation($"Bot @{me.Username} is now running and receiving updates!");

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Fire-and-forget: Process update in background to avoid blocking other updates
        // This allows multiple updates to be processed in parallel
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogDebug("Received update type: {UpdateType}, UpdateId: {UpdateId}", update.Type, update.Id);
                
                using var scope = _serviceProvider.CreateScope();
                var updateHandler = scope.ServiceProvider.GetRequiredService<TelegramUpdateHandler>();
                var paymentHandler = scope.ServiceProvider.GetRequiredService<PaymentCallbackHandler>();

                switch (update.Type)
                {
                    case UpdateType.Message:
                        if (update.Message != null)
                        {
                            _logger.LogDebug("Processing message from user {UserId}, text: {Text}", 
                                update.Message.From?.Id, 
                                update.Message.Text ?? "(no text)");
                            
                            // Check if it's a successful payment first
                            if (update.Message.SuccessfulPayment != null)
                            {
                                var payment = update.Message.SuccessfulPayment;
                                await paymentHandler.HandleSuccessfulPaymentAsync(
                                    payment.TelegramPaymentChargeId,
                                    null,
                                    update.Message.From!.Id,
                                    payment.InvoicePayload,
                                    payment.TotalAmount,
                                    payment.Currency,
                                    update.Message.Chat.Id,
                                    cancellationToken);
                            }
                            else if (update.Message.From != null)
                            {
                                // Regular message
                                var telegramMessage = new TelegramMessage
                                {
                                    MessageId = update.Message.MessageId,
                                    From = new TelegramUser
                                    {
                                        Id = update.Message.From.Id,
                                        IsBot = update.Message.From.IsBot,
                                        FirstName = update.Message.From.FirstName,
                                        LastName = update.Message.From.LastName,
                                        Username = update.Message.From.Username,
                                        LanguageCode = update.Message.From.LanguageCode
                                    },
                                    ChatId = update.Message.Chat.Id,
                                    Text = update.Message.Text,
                                    Date = update.Message.Date,
                                    Photo = update.Message.Photo,
                                    Video = update.Message.Video
                                };

                                _logger.LogDebug("Calling HandleMessageAsync for message: {Text}", telegramMessage.Text ?? "(no text)");
                                await updateHandler.HandleMessageAsync(telegramMessage, cancellationToken);
                                _logger.LogDebug("HandleMessageAsync completed successfully");
                            }
                            else
                            {
                                _logger.LogWarning("Message received but From is null");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Update type is Message but Message is null");
                        }
                        break;

                    case UpdateType.CallbackQuery:
                        if (update.CallbackQuery != null && update.CallbackQuery.Data != null)
                        {
                            _logger.LogDebug("Processing callback query: {Data}", update.CallbackQuery.Data);
                            
                            // Answer the callback query to remove the loading state
                            await _botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, cancellationToken: cancellationToken);
                            
                            // Delegate to update handler
                            await updateHandler.HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
                        }
                        break;

                    case UpdateType.PreCheckoutQuery:
                        if (update.PreCheckoutQuery != null)
                        {
                            await paymentHandler.HandlePreCheckoutQueryAsync(
                                update.PreCheckoutQuery.Id,
                                update.PreCheckoutQuery.From.Id,
                                update.PreCheckoutQuery.InvoicePayload,
                                update.PreCheckoutQuery.TotalAmount,
                                update.PreCheckoutQuery.Currency,
                                cancellationToken);
                        }
                        break;

                    case UpdateType.MessageReaction:
                        if (update.MessageReaction != null)
                        {
                            _logger.LogDebug("Processing message reaction from user {UserId}, chat {ChatId}, message {MessageId}", 
                                update.MessageReaction.User?.Id ?? update.MessageReaction.ActorChat?.Id ?? 0,
                                update.MessageReaction.Chat.Id,
                                update.MessageReaction.MessageId);
                            
                            await updateHandler.HandleMessageReactionAsync(update.MessageReaction, cancellationToken);
                        }
                        else
                        {
                            _logger.LogWarning("Update type is MessageReaction but MessageReaction is null");
                        }
                        break;

                    default:
                        _logger.LogWarning("Unhandled update type: {UpdateType}", update.Type);
                        break;

                    // Note: MessageReaction support requires Telegram.Bot v21.0.0 or higher
                    // For now, Star Reaction payments will use manual confirmation via buttons
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update. UpdateType: {UpdateType}, UpdateId: {UpdateId}, Exception: {ExceptionMessage}, StackTrace: {StackTrace}", 
                    update.Type, 
                    update.Id, 
                    ex.Message, 
                    ex.StackTrace);
            }
        }, cancellationToken);

        // Return immediately to allow next update to be processed
        return Task.CompletedTask;
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram polling error occurred");
        return Task.CompletedTask;
    }
}

