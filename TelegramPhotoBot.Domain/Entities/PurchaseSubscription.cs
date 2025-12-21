using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public class PurchaseSubscription : Purchase
{
    public Guid SubscriptionId { get; private set; }
    
    // Navigation properties
    public virtual Subscription Subscription { get; private set; } = null!;

    // EF Core constructor
    protected PurchaseSubscription() { }

    public PurchaseSubscription(Guid userId, Guid subscriptionId, TelegramStars amount)
        : base(userId, amount)
    {
        SubscriptionId = subscriptionId;
    }

    public override PurchaseType GetPurchaseType() => PurchaseType.Subscription;
}

