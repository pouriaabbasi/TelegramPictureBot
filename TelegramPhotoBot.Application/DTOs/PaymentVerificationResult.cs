namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Result of payment verification
/// </summary>
public class PaymentVerificationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid PurchaseId { get; init; }

    public static PaymentVerificationResult Success(Guid purchaseId) => new()
    {
        IsValid = true,
        PurchaseId = purchaseId
    };

    public static PaymentVerificationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

