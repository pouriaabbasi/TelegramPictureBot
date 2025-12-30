using TelegramPhotoBot.Domain.ValueObjects;
using FileInfo = TelegramPhotoBot.Domain.ValueObjects.FileInfo;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Represents a content creator (Model) in the marketplace
/// </summary>
public class Model : AggregateRoot
{
    // Navigation property to User
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    // Model profile
    public string DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public string? Alias { get; private set; } // Non-unique nickname for the model
    
    // Demo/preview content
    public FileInfo? DemoImage { get; private set; }
    
    // Pricing
    public TelegramStars? SubscriptionPrice { get; private set; }
    public int? SubscriptionDurationDays { get; private set; }
    
    // Status
    public ModelStatus Status { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByAdminId { get; private set; }
    public string? RejectionReason { get; private set; }
    
    // Metrics
    public int TotalSubscribers { get; private set; }
    public int TotalContentItems { get; private set; }

    // Navigation properties
    public ICollection<Photo> Photos { get; private set; } = new List<Photo>();
    public ICollection<ModelSubscription> Subscriptions { get; private set; } = new List<ModelSubscription>();

    // Private constructor for EF Core
    private Model() 
    {
        DisplayName = string.Empty;
    }

    public Model(Guid userId, string displayName, string? bio = null)
    {
        UserId = userId;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Bio = bio;
        Status = ModelStatus.PendingApproval;
        TotalSubscribers = 0;
        TotalContentItems = 0;
    }

    // Business logic methods
    
    public void UpdateProfile(string displayName, string? bio)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));
            
        DisplayName = displayName;
        Bio = bio;
        MarkAsUpdated();
    }

    public void SetAlias(string? alias)
    {
        Alias = alias;
        MarkAsUpdated();
    }

    public void SetDemoImage(FileInfo demoImage)
    {
        DemoImage = demoImage ?? throw new ArgumentNullException(nameof(demoImage));
        MarkAsUpdated();
    }

    public void RemoveDemoImage()
    {
        DemoImage = null;
        MarkAsUpdated();
    }

    public void SetSubscriptionPricing(TelegramStars price, int durationDays)
    {
        if (durationDays <= 0)
            throw new ArgumentException("Duration must be positive", nameof(durationDays));
            
        SubscriptionPrice = price ?? throw new ArgumentNullException(nameof(price));
        SubscriptionDurationDays = durationDays;
        MarkAsUpdated();
    }

    public void Approve(Guid approvedByAdminId)
    {
        if (Status == ModelStatus.Approved)
            throw new InvalidOperationException("Model is already approved");
            
        Status = ModelStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedByAdminId = approvedByAdminId;
        RejectionReason = null;
        MarkAsUpdated();
    }

    public void Reject(Guid rejectedByAdminId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required", nameof(reason));
            
        Status = ModelStatus.Rejected;
        RejectionReason = reason;
        MarkAsUpdated();
    }

    public void Suspend(string reason)
    {
        Status = ModelStatus.Suspended;
        RejectionReason = reason;
        MarkAsUpdated();
    }

    public void Reactivate()
    {
        if (Status != ModelStatus.Suspended)
            throw new InvalidOperationException("Only suspended models can be reactivated");
            
        Status = ModelStatus.Approved;
        RejectionReason = null;
        MarkAsUpdated();
    }

    public void IncrementSubscribers()
    {
        TotalSubscribers++;
        MarkAsUpdated();
    }

    public void DecrementSubscribers()
    {
        if (TotalSubscribers > 0)
            TotalSubscribers--;
        MarkAsUpdated();
    }

    public void IncrementContentItems()
    {
        TotalContentItems++;
        MarkAsUpdated();
    }

    public bool CanAcceptSubscriptions()
    {
        return Status == ModelStatus.Approved 
            && SubscriptionPrice != null 
            && SubscriptionDurationDays.HasValue;
    }

    public bool CanSellContent()
    {
        return Status == ModelStatus.Approved;
    }
}

public enum ModelStatus
{
    PendingApproval,
    Approved,
    Rejected,
    Suspended
}

