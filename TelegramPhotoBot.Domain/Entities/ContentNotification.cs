using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Tracks notifications sent to users about new content
/// </summary>
public class ContentNotification : BaseEntity
{
    public Guid ContentId { get; private set; }
    public Guid ModelId { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    // Navigation properties
    public virtual Photo? Content { get; private set; }
    public virtual Model? Model { get; private set; }
    public virtual User? User { get; private set; }

    // EF Core constructor
    private ContentNotification() { }

    public ContentNotification(
        Guid contentId,
        Guid modelId,
        Guid userId)
    {
        ContentId = contentId;
        ModelId = modelId;
        UserId = userId;
        Status = NotificationStatus.Pending;
        RetryCount = 0;
    }

    public void MarkAsSending()
    {
        Status = NotificationStatus.Sending;
        MarkAsUpdated();
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        MarkAsUpdated();
    }

    public bool CanRetry(int maxRetries = 3)
    {
        return Status == NotificationStatus.Failed && RetryCount < maxRetries;
    }

    public void ResetForRetry()
    {
        Status = NotificationStatus.Pending;
        ErrorMessage = null;
        MarkAsUpdated();
    }
}
