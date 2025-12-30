namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Tracks the contact verification status between sender and recipient
/// </summary>
public class UserContactVerification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Whether the user was successfully auto-added to sender's contacts
    /// </summary>
    public bool IsAutoAddedToSenderContacts { get; set; }
    
    /// <summary>
    /// Whether mutual contact has been established (both parties have each other)
    /// </summary>
    public bool IsMutualContact { get; set; }
    
    /// <summary>
    /// Whether admin has been notified about manual add requirement
    /// </summary>
    public bool IsAdminNotified { get; set; }
    
    /// <summary>
    /// Whether the user has been instructed to add sender and send a message
    /// </summary>
    public bool IsUserInstructedToAddContact { get; set; }
    
    /// <summary>
    /// Whether user has sent at least one message (indicating they added the contact)
    /// </summary>
    public bool HasUserSentMessage { get; set; }
    
    /// <summary>
    /// Last check timestamp
    /// </summary>
    public DateTime LastCheckedAt { get; set; }
    
    /// <summary>
    /// Last error message if verification failed
    /// </summary>
    public string? LastErrorMessage { get; set; }
}

