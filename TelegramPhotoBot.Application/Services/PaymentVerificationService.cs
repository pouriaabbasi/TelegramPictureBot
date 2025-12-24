using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Services;

public class PaymentVerificationService : IPaymentVerificationService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentVerificationService(
        IPurchaseRepository purchaseRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> IsPaymentAlreadyProcessedAsync(string telegramPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(telegramPaymentId))
            return false;

        var existingPurchase = await _purchaseRepository.GetByTelegramPaymentIdAsync(telegramPaymentId, cancellationToken);
        return existingPurchase != null && existingPurchase.IsPaymentCompleted();
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        PaymentVerificationRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.TelegramPaymentId))
        {
            return PaymentVerificationResult.Failure("Telegram payment ID is required");
        }

        // Check for duplicate payment processing
        var isAlreadyProcessed = await IsPaymentAlreadyProcessedAsync(request.TelegramPaymentId, cancellationToken);
        if (isAlreadyProcessed)
        {
            return PaymentVerificationResult.Failure("This payment has already been processed");
        }

        // Get the purchase
        var purchase = await _purchaseRepository.GetByIdAsync(request.PurchaseId, cancellationToken);
        if (purchase == null)
        {
            return PaymentVerificationResult.Failure("Purchase not found");
        }

        // Verify payment details match - get user to compare TelegramUserId
        var user = await _userRepository.GetByIdAsync(purchase.UserId, cancellationToken);
        if (user == null)
        {
            return PaymentVerificationResult.Failure("User not found");
        }

        if (user.TelegramUserId.Value != request.TelegramUserId)
        {
            return PaymentVerificationResult.Failure("Payment user ID does not match purchase user ID");
        }

        if (purchase.Amount.Amount != request.Amount)
        {
            return PaymentVerificationResult.Failure("Payment amount does not match purchase amount");
        }

        // Verify currency (Telegram Stars)
        if (request.Currency != "XTR")
        {
            return PaymentVerificationResult.Failure("Only Telegram Stars (XTR) payments are accepted");
        }

        // Mark payment as completed
        try
        {
            purchase.MarkPaymentCompleted(request.TelegramPaymentId, request.PreCheckoutQueryId);
            await _purchaseRepository.UpdateAsync(purchase, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return PaymentVerificationResult.Success(purchase.Id);
        }
        catch (Exception ex)
        {
            return PaymentVerificationResult.Failure($"Failed to process payment: {ex.Message}");
        }
    }
}

