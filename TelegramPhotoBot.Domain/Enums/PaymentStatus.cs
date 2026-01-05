namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Status of a pending star payment
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending, waiting for stars
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Payment completed successfully
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// Payment expired (timeout)
    /// </summary>
    Expired = 2,
    
    /// <summary>
    /// Payment cancelled by user
    /// </summary>
    Cancelled = 3,
    
    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 4
}
