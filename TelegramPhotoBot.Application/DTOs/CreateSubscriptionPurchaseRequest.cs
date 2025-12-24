namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Request for creating a subscription purchase
/// </summary>
public class CreateSubscriptionPurchaseRequest
{
    public Guid UserId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
}

