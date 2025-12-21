using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid SubscriptionPlanId { get; private set; }
    public DateRange Period { get; private set; }
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.Active;
    public TelegramStars PaidAmount { get; private set; }
    
    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual SubscriptionPlan SubscriptionPlan { get; private set; } = null!;

    // EF Core constructor
    protected Subscription() { }

    public Subscription(
        Guid userId,
        Guid subscriptionPlanId,
        DateRange period,
        TelegramStars paidAmount)
    {
        UserId = userId;
        SubscriptionPlanId = subscriptionPlanId;
        Period = period;
        PaidAmount = paidAmount;
    }

    public bool IsActive()
    {
        return Status == SubscriptionStatus.Active && Period.IsActive();
    }

    public bool IsExpired()
    {
        return Period.IsExpired();
    }

    public void Expire()
    {
        if (Status == SubscriptionStatus.Active)
        {
            Status = SubscriptionStatus.Expired;
            MarkAsUpdated();
        }
    }

    public void Cancel()
    {
        if (Status == SubscriptionStatus.Active)
        {
            Status = SubscriptionStatus.Cancelled;
            MarkAsUpdated();
        }
    }

    public int DaysRemaining()
    {
        return Period.DaysRemaining();
    }
}
