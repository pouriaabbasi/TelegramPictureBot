using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Tracks pending star reaction payments
/// </summary>
public class PendingStarPayment : BaseEntity
{
    public Guid UserId { get; private set; }
    public long TelegramUserId { get; private set; }
    public Guid ContentId { get; private set; }
    public ContentType ContentType { get; private set; }
    public int RequiredStars { get; private set; }
    public int ReceivedStars { get; private set; }
    public long PaymentMessageId { get; private set; }
    public long ChatId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    // Navigation properties
    public virtual User? User { get; private set; }
    public virtual Photo? Photo { get; private set; }

    // EF Core constructor
    private PendingStarPayment() { }

    public PendingStarPayment(
        Guid userId,
        long telegramUserId,
        Guid contentId,
        ContentType contentType,
        int requiredStars,
        long paymentMessageId,
        long chatId,
        DateTime expiresAt)
    {
        UserId = userId;
        TelegramUserId = telegramUserId;
        ContentId = contentId;
        ContentType = contentType;
        RequiredStars = requiredStars;
        ReceivedStars = 0;
        PaymentMessageId = paymentMessageId;
        ChatId = chatId;
        Status = PaymentStatus.Pending;
        ExpiresAt = expiresAt;
    }

    public bool IsComplete() => ReceivedStars >= RequiredStars;
    
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt && Status == PaymentStatus.Pending;
    
    public void AddStars(int count)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot add stars to payment with status {Status}");
            
        ReceivedStars += count;
        
        if (IsComplete())
        {
            Status = PaymentStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
        
        MarkAsUpdated();
    }
    
    public void MarkAsCompleted()
    {
        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
    
    public void MarkAsExpired()
    {
        Status = PaymentStatus.Expired;
        MarkAsUpdated();
    }
    
    public void MarkAsCancelled()
    {
        Status = PaymentStatus.Cancelled;
        MarkAsUpdated();
    }
    
    public void MarkAsFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        MarkAsUpdated();
    }
}
