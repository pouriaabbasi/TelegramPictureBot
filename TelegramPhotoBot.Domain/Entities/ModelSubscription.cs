using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Represents a user's subscription to a specific model's content
/// Replaces the global Subscription entity with model-scoped subscriptions
/// </summary>
public class ModelSubscription : Purchase
{
    // Model scoping
    public Guid ModelId { get; private set; }
    public Model Model { get; private set; } = null!;
    
    // Subscription period
    public DateRange SubscriptionPeriod { get; private set; }
    
    // Status
    public bool IsActive { get; private set; }
    public bool AutoRenew { get; private set; }

    // Private constructor for EF Core
    private ModelSubscription() : base()
    {
        SubscriptionPeriod = null!;
    }

    public ModelSubscription(
        Guid userId,
        Guid modelId,
        DateRange subscriptionPeriod,
        TelegramStars amount) : base(userId, amount)
    {
        ModelId = modelId;
        SubscriptionPeriod = subscriptionPeriod ?? throw new ArgumentNullException(nameof(subscriptionPeriod));
        IsActive = true;
        AutoRenew = false;
    }

    public override PurchaseType GetPurchaseType()
    {
        return PurchaseType.Subscription;
    }

    // Business logic methods

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void EnableAutoRenew()
    {
        AutoRenew = true;
        MarkAsUpdated();
    }

    public void DisableAutoRenew()
    {
        AutoRenew = false;
        MarkAsUpdated();
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > SubscriptionPeriod.EndDate;
    }

    public bool IsValidNow()
    {
        return IsActive && !IsExpired();
    }

    public void CheckAndUpdateExpiration()
    {
        if (IsExpired() && IsActive)
        {
            Deactivate();
        }
    }

    public int GetRemainingDays()
    {
        if (IsExpired())
            return 0;

        var remaining = SubscriptionPeriod.EndDate - DateTime.UtcNow;
        return Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
    }
}

