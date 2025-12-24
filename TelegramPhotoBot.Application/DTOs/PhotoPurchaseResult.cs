namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Result of creating a photo purchase
/// </summary>
public class PhotoPurchaseResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid PurchaseId { get; init; }
    public long Amount { get; init; }

    public static PhotoPurchaseResult Success(Guid purchaseId, long amount) => new()
    {
        IsSuccess = true,
        PurchaseId = purchaseId,
        Amount = amount
    };

    public static PhotoPurchaseResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

