namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Status of a content notification
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification is pending and waiting to be sent
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Notification has been sent successfully
    /// </summary>
    Sent = 1,
    
    /// <summary>
    /// Notification failed to send
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Notification is being sent (in progress)
    /// </summary>
    Sending = 3
}
