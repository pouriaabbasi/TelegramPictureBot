using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public class SubscriptionPlan : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public TelegramStars Price { get; private set; }
    public int DurationDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByAdminId { get; private set; }
    
    // Navigation properties
    public virtual User CreatedByAdmin { get; private set; } = null!;
    
    private readonly List<Subscription> _subscriptions = new();
    public virtual IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();

    // EF Core constructor
    protected SubscriptionPlan() { }

    public SubscriptionPlan(
        string name,
        string description,
        TelegramStars price,
        int durationDays,
        Guid createdByAdminId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (durationDays <= 0)
            throw new ArgumentException("Duration days must be greater than zero", nameof(durationDays));

        Name = name;
        Description = description;
        Price = price;
        DurationDays = durationDays;
        CreatedByAdminId = createdByAdminId;
    }

    public void UpdateDetails(string name, string description, TelegramStars price, int durationDays)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (durationDays <= 0)
            throw new ArgumentException("Duration days must be greater than zero", nameof(durationDays));

        Name = name;
        Description = description;
        Price = price;
        DurationDays = durationDays;
        MarkAsUpdated();
    }

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
}
