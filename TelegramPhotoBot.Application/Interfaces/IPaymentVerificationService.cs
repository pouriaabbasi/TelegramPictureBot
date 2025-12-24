using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service responsible for verifying Telegram Stars payments and handling payment callbacks
/// </summary>
public interface IPaymentVerificationService
{
    /// <summary>
    /// Verifies a Telegram Stars payment and marks the purchase as completed
    /// </summary>
    /// <param name="request">Payment verification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    Task<PaymentVerificationResult> VerifyPaymentAsync(PaymentVerificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment ID has already been processed (prevents duplicate processing)
    /// </summary>
    Task<bool> IsPaymentAlreadyProcessedAsync(string telegramPaymentId, CancellationToken cancellationToken = default);
}

