namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Result of creating a subscription purchase
/// </summary>
public class SubscriptionPurchaseResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid PurchaseId { get; init; }
    public Guid SubscriptionId { get; init; }
    public long Amount { get; init; }

    public static SubscriptionPurchaseResult Success(Guid purchaseId, Guid subscriptionId, long amount) => new()
    {
        IsSuccess = true,
        PurchaseId = purchaseId,
        SubscriptionId = subscriptionId,
        Amount = amount
    };

    public static SubscriptionPurchaseResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

