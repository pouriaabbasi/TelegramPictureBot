using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Implementation of Telegram Bot API service
/// </summary>
public class TelegramBotService : ITelegramBotService
{
    private readonly ITelegramBotClient _botClient;

    public TelegramBotService(string botToken)
    {
        if (string.IsNullOrWhiteSpace(botToken))
            throw new ArgumentNullException(nameof(botToken));
        
        _botClient = new TelegramBotClient(botToken);
    }

    public async Task<bool> SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendMessageWithButtonsAsync(long chatId, string message, object keyboard, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                replyMarkup: keyboard as InlineKeyboardMarkup,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message with buttons: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EditMessageTextAsync(long chatId, int messageId, string newText, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: newText,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing message: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EditMessageTextAndRemoveKeyboardAsync(long chatId, int messageId, string newText, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: newText,
                replyMarkup: null,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error editing message and removing keyboard: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendContactAsync(long chatId, string phoneNumber, string firstName, string? lastName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.SendContactAsync(
                chatId: chatId,
                phoneNumber: phoneNumber,
                firstName: firstName,
                lastName: lastName,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending contact: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendPhotoAsync(long chatId, string photoPathOrFileId, string? caption = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if it's a Telegram file ID (doesn't contain path separators and doesn't exist as a file)
            if (!photoPathOrFileId.Contains(Path.DirectorySeparatorChar) && 
                !photoPathOrFileId.Contains(Path.AltDirectorySeparatorChar) &&
                !System.IO.File.Exists(photoPathOrFileId))
            {
                // It's a Telegram file ID - use it directly
                Console.WriteLine($"Sending photo using Telegram file ID: {photoPathOrFileId.Substring(0, Math.Min(20, photoPathOrFileId.Length))}...");
                
                await _botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: InputFile.FromFileId(photoPathOrFileId),
                    caption: caption,
                    cancellationToken: cancellationToken);
                
                return true;
            }
            else
            {
                // It's a file path - open and stream the file
                if (!System.IO.File.Exists(photoPathOrFileId))
                {
                    Console.WriteLine($"Photo file not found: {photoPathOrFileId}");
                    return false;
                }
                
                Console.WriteLine($"Sending photo from file path: {photoPathOrFileId}");
                
                // Keep stream open during the entire send operation
                using var stream = System.IO.File.OpenRead(photoPathOrFileId);
                await _botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: InputFile.FromStream(stream, Path.GetFileName(photoPathOrFileId)),
                    caption: caption,
                    cancellationToken: cancellationToken);
                
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending photo: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<string?> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var prices = new List<LabeledPrice>();
            if (request.Prices != null)
            {
                foreach (var price in request.Prices)
                {
                    prices.Add(new LabeledPrice(price.Key, (int)long.Parse(price.Value)));
                }
            }
            else
            {
                prices.Add(new LabeledPrice("Total", (int)request.Amount));
            }

            var message = await _botClient.SendInvoiceAsync(
                chatId: request.ChatId,
                title: request.Title,
                description: request.Description,
                payload: request.Payload,
                providerToken: "", // Empty for Telegram Stars
                currency: request.Currency,
                prices: prices,
                cancellationToken: cancellationToken);

            return message.MessageId.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating invoice: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> AnswerPreCheckoutQueryAsync(
        string preCheckoutQueryId, 
        bool ok, 
        string? errorMessage = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (ok)
            {
                await _botClient.AnswerPreCheckoutQueryAsync(
                    preCheckoutQueryId: preCheckoutQueryId,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.AnswerPreCheckoutQueryAsync(
                    preCheckoutQueryId: preCheckoutQueryId,
                    errorMessage: errorMessage ?? "Payment validation failed",
                    cancellationToken: cancellationToken);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error answering pre-checkout query: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> VerifyPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        // Telegram Bot API doesn't have a direct verification endpoint
        // Payment verification is done through webhook callbacks
        // This method can be used for additional validation if needed
        
        await Task.CompletedTask;
        return true;
    }
}
