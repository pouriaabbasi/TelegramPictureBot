using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Handler for Telegram payment callbacks (pre-checkout queries and successful payments)
/// </summary>
public class PaymentCallbackHandler
{
    private readonly IPaymentVerificationService _paymentVerificationService;
    private readonly ITelegramBotService _telegramBotService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IContentDeliveryService _contentDeliveryService;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUserService _userService;

    public PaymentCallbackHandler(
        IPaymentVerificationService paymentVerificationService,
        ITelegramBotService telegramBotService,
        ISubscriptionService subscriptionService,
        IContentDeliveryService contentDeliveryService,
        IPhotoRepository photoRepository,
        IPurchaseRepository purchaseRepository,
        IUserService userService)
    {
        _paymentVerificationService = paymentVerificationService ?? throw new ArgumentNullException(nameof(paymentVerificationService));
        _telegramBotService = telegramBotService ?? throw new ArgumentNullException(nameof(telegramBotService));
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        _contentDeliveryService = contentDeliveryService ?? throw new ArgumentNullException(nameof(contentDeliveryService));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// Handles a pre-checkout query (before payment completion)
    /// </summary>
    public async Task HandlePreCheckoutQueryAsync(
        string preCheckoutQueryId,
        long telegramUserId,
        string payload,
        long totalAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // Parse payload to get purchase ID
        if (!Guid.TryParse(payload, out var purchaseId))
        {
            await _telegramBotService.AnswerPreCheckoutQueryAsync(
                preCheckoutQueryId,
                ok: false,
                errorMessage: "Invalid purchase information.",
                cancellationToken);
            return;
        }

        // Get purchase
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase == null)
        {
            await _telegramBotService.AnswerPreCheckoutQueryAsync(
                preCheckoutQueryId,
                ok: false,
                errorMessage: "Purchase not found.",
                cancellationToken);
            return;
        }

        // Validate payment details - need to get user to compare TelegramUserId
        // For now, we'll validate amount and currency, user validation happens in PaymentVerificationService
        {
            await _telegramBotService.AnswerPreCheckoutQueryAsync(
                preCheckoutQueryId,
                ok: false,
                errorMessage: "Payment user mismatch.",
                cancellationToken);
            return;
        }

        if (purchase.Amount.Amount != totalAmount)
        {
            await _telegramBotService.AnswerPreCheckoutQueryAsync(
                preCheckoutQueryId,
                ok: false,
                errorMessage: "Payment amount mismatch.",
                cancellationToken);
            return;
        }

        if (currency != "XTR")
        {
            await _telegramBotService.AnswerPreCheckoutQueryAsync(
                preCheckoutQueryId,
                ok: false,
                errorMessage: "Only Telegram Stars (XTR) are accepted.",
                cancellationToken);
            return;
        }

        // Approve the pre-checkout query
        await _telegramBotService.AnswerPreCheckoutQueryAsync(
            preCheckoutQueryId,
            ok: true,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Handles a successful payment
    /// </summary>
    public async Task HandleSuccessfulPaymentAsync(
        string telegramPaymentId,
        string? preCheckoutQueryId,
        long telegramUserId,
        string payload,
        long totalAmount,
        string currency,
        long chatId,
        CancellationToken cancellationToken = default)
    {
        // Parse payload to get purchase ID
        if (!Guid.TryParse(payload, out var purchaseId))
        {
            await _telegramBotService.SendMessageAsync(
                chatId,
                "Error: Invalid purchase information.",
                cancellationToken);
            return;
        }

        // Verify payment
        var verificationRequest = new PaymentVerificationRequest
        {
            TelegramPaymentId = telegramPaymentId,
            PreCheckoutQueryId = preCheckoutQueryId,
            PurchaseId = purchaseId,
            TelegramUserId = telegramUserId,
            Amount = totalAmount,
            Currency = currency
        };

        var verificationResult = await _paymentVerificationService.VerifyPaymentAsync(verificationRequest, cancellationToken);

        if (!verificationResult.IsValid)
        {
            await _telegramBotService.SendMessageAsync(
                chatId,
                $"Payment verification failed: {verificationResult.ErrorMessage}",
                cancellationToken);
            return;
        }

        // Get purchase to determine type
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase == null)
        {
            await _telegramBotService.SendMessageAsync(chatId, "Purchase not found.", cancellationToken);
            return;
        }

        // Handle based on purchase type
        switch (purchase.GetPurchaseType())
        {
            case PurchaseType.Subscription:
                await HandleSubscriptionPurchaseCompletionAsync(purchaseId, chatId, cancellationToken);
                break;

            case PurchaseType.SinglePhoto:
                await HandlePhotoPurchaseCompletionAsync(purchaseId, chatId, cancellationToken);
                break;

            default:
                await _telegramBotService.SendMessageAsync(chatId, "Payment completed successfully!", cancellationToken);
                break;
        }
    }

    private async Task HandleSubscriptionPurchaseCompletionAsync(
        Guid purchaseId,
        long chatId,
        CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase is PurchaseSubscription subscriptionPurchase)
        {
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(
                purchase.UserId,
                cancellationToken);

            if (subscription != null)
            {
                var message = $"✅ Subscription activated!\n\n" +
                             $"Plan: {subscription.PlanName}\n" +
                             $"Valid until: {subscription.EndDate:yyyy-MM-dd}\n" +
                             $"Days remaining: {subscription.DaysRemaining}";

                await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
            }
        }
    }

    private async Task HandlePhotoPurchaseCompletionAsync(
        Guid purchaseId,
        long chatId,
        CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase is PurchasePhoto photoPurchase)
        {
            var photo = await _photoRepository.GetByIdAsync(photoPurchase.PhotoId, cancellationToken);
            if (photo != null)
            {
                // Get user
                var user = await _userService.GetUserByTelegramIdAsync(chatId, cancellationToken);
                if (user != null)
                {
                    // Send photo via MTProto
                    var sendRequest = new SendPhotoRequest
                    {
                        RecipientTelegramUserId = user.TelegramUserId,
                        FilePath = photo.FileInfo.FilePath,
                        Caption = photo.Caption,
                        SelfDestructSeconds = 60
                    };

                    var deliveryResult = await _contentDeliveryService.SendPhotoAsync(sendRequest, cancellationToken);

                    if (deliveryResult.IsSuccess)
                    {
                        await _telegramBotService.SendMessageAsync(
                            chatId,
                            "✅ Payment completed! Your photo has been sent.",
                            cancellationToken);
                    }
                    else
                    {
                        await _telegramBotService.SendMessageAsync(
                            chatId,
                            $"✅ Payment completed! However, content delivery failed: {deliveryResult.ErrorMessage}",
                            cancellationToken);
                    }
                }
            }
        }
    }
}

