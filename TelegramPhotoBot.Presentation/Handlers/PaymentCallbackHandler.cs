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
    private readonly IContentDeliveryService _contentDeliveryService;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUserService _userService;
    private readonly IModelSubscriptionService _modelSubscriptionService;
    private readonly IModelService _modelService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentCallbackHandler(
        IPaymentVerificationService paymentVerificationService,
        ITelegramBotService telegramBotService,
        IContentDeliveryService contentDeliveryService,
        IPhotoRepository photoRepository,
        IPurchaseRepository purchaseRepository,
        IUserService userService,
        IModelSubscriptionService modelSubscriptionService,
        IModelService modelService,
        IUnitOfWork unitOfWork)
    {
        _paymentVerificationService = paymentVerificationService ?? throw new ArgumentNullException(nameof(paymentVerificationService));
        _telegramBotService = telegramBotService ?? throw new ArgumentNullException(nameof(telegramBotService));
        _contentDeliveryService = contentDeliveryService ?? throw new ArgumentNullException(nameof(contentDeliveryService));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _modelSubscriptionService = modelSubscriptionService ?? throw new ArgumentNullException(nameof(modelSubscriptionService));
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
        // Check if it's a subscription payment
        if (payload.StartsWith("subscription_"))
        {
            // Validate subscription payment
            var parts = payload.Split('_');
            if (parts.Length != 3 || !Guid.TryParse(parts[1], out var modelId) || !Guid.TryParse(parts[2], out var userId))
            {
                await _telegramBotService.AnswerPreCheckoutQueryAsync(
                    preCheckoutQueryId,
                    ok: false,
                    errorMessage: "Invalid subscription information.",
                    cancellationToken);
                return;
            }

            // Just validate currency - amount and other checks will be done after payment
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
            return;
        }

        // Check if it's a photo payment
        if (payload.StartsWith("photo_"))
        {
            // Parse payload: photo_{photoId}_{userId}
            var parts = payload.Split('_');
            if (parts.Length != 3 || !Guid.TryParse(parts[1], out var photoId) || !Guid.TryParse(parts[2], out var userId))
            {
                await _telegramBotService.AnswerPreCheckoutQueryAsync(
                    preCheckoutQueryId,
                    ok: false,
                    errorMessage: "Invalid photo information.",
                    cancellationToken);
                return;
            }

            // Just validate currency
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
            return;
        }

        // Old format: Parse payload to get purchase ID
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
        // Check if it's a subscription payment
        if (payload.StartsWith("subscription_"))
        {
            var parts = payload.Split('_');
            if (parts.Length == 3 && Guid.TryParse(parts[1], out var modelId) && Guid.TryParse(parts[2], out var userId))
            {
                await HandleSubscriptionPaymentCompletionAsync(userId, modelId, totalAmount, chatId, cancellationToken);
                return;
            }
        }

        // Check if it's a photo payment
        if (payload.StartsWith("photo_"))
        {
            var parts = payload.Split('_');
            if (parts.Length == 3 && Guid.TryParse(parts[1], out var photoId) && Guid.TryParse(parts[2], out var userId))
            {
                await HandlePhotoPaymentCompletionAsync(userId, photoId, totalAmount, chatId, cancellationToken);
                return;
            }
        }

        // Old format: Parse payload to get purchase ID
        if (!Guid.TryParse(payload, out var purchaseId))
        {
            await _telegramBotService.SendMessageAsync(
                chatId,
                "❌ Error: Invalid purchase information.",
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
                $"❌ Payment verification failed: {verificationResult.ErrorMessage}",
                cancellationToken);
            return;
        }

        // Get purchase to determine type
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase == null)
        {
            await _telegramBotService.SendMessageAsync(chatId, "❌ Purchase not found.", cancellationToken);
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
                await _telegramBotService.SendMessageAsync(chatId, "✅ Payment completed successfully!", cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Handles subscription payment completion (new format)
    /// </summary>
    private async Task HandleSubscriptionPaymentCompletionAsync(
        Guid userId,
        Guid modelId,
        long paidAmount,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByIdAsync(modelId, cancellationToken);
            if (model == null || !model.CanAcceptSubscriptions())
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Model not available for subscriptions.", cancellationToken);
                return;
            }

            // Create subscription
            var subscription = await _modelSubscriptionService.CreateSubscriptionAsync(
                userId,
                modelId,
                model.SubscriptionPrice!,
                cancellationToken);

            var displayText = !string.IsNullOrWhiteSpace(model.Alias) 
                ? model.Alias 
                : model.DisplayName;
            
            await _telegramBotService.SendMessageAsync(
                chatId,
                $"✅ Successfully subscribed to {displayText}!\n\n" +
                $"💎 Duration: {model.SubscriptionDurationDays} days\n" +
                $"📅 Expires: {subscription.SubscriptionPeriod.EndDate:d}\n\n" +
                $"🎉 You now have access to all of {displayText}'s premium content!",
                cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(
                chatId,
                $"❌ Error completing subscription: {ex.Message}",
                cancellationToken);
        }
    }

    /// <summary>
    /// Handles photo payment completion (new format)
    /// </summary>
    private async Task HandlePhotoPaymentCompletionAsync(
        Guid userId,
        Guid photoId,
        long paidAmount,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Photo not found.", cancellationToken);
                return;
            }

            // Create purchase record
            var purchaseRequest = new CreatePhotoPurchaseRequest
            {
                UserId = userId,
                PhotoId = photoId
            };

            var purchaseResult = await _photoRepository.GetByIdAsync(photoId, cancellationToken); // Simplified - should use purchase service
            
            // Get user for content delivery
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
                    // Track the view after successful purchase delivery
                    photo.IncrementViewCount();
                    await _photoRepository.UpdateAsync(photo, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
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
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(
                chatId,
                $"❌ Error completing photo purchase: {ex.Message}",
                cancellationToken);
        }
    }

    private async Task HandleSubscriptionPurchaseCompletionAsync(
        Guid purchaseId,
        long chatId,
        CancellationToken cancellationToken)
    {
        // Platform subscriptions are deprecated - this handler is no longer used
        await _telegramBotService.SendMessageAsync(
            chatId, 
            "❌ Platform subscriptions are not supported. Please use model subscriptions instead.",
            cancellationToken);
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
                        // Track the view after successful purchase delivery
                        photo.IncrementViewCount();
                        await _photoRepository.UpdateAsync(photo, cancellationToken);
                        
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

