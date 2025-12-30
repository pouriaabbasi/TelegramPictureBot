using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces;

public interface IContactVerificationService
{
    /// <summary>
    /// Verifies and ensures mutual contact between sender and recipient.
    /// Returns detailed result about the contact status and required actions.
    /// </summary>
    Task<ContactVerificationResult> VerifyAndEnsureMutualContactAsync(
        User recipientUser, 
        long recipientTelegramUserId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks that user has sent a message (indicating they likely added the contact)
    /// </summary>
    Task MarkUserSentMessageAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the sender's username or phone for sharing with recipients
    /// </summary>
    Task<string?> GetSenderContactInfoAsync(CancellationToken cancellationToken = default);
}

public class ContactVerificationResult
{
    public bool IsMutualContact { get; set; }
    public bool IsAutoAddedToSenderContacts { get; set; }
    public bool RequiresManualAction { get; set; }
    public string? UserInstructionMessage { get; set; }
    public string? AdminNotificationMessage { get; set; }
    public bool ShouldNotifyAdmin { get; set; }
    public string? ErrorMessage { get; set; }

    public static ContactVerificationResult Success()
    {
        return new ContactVerificationResult
        {
            IsMutualContact = true,
            IsAutoAddedToSenderContacts = true,
            RequiresManualAction = false
        };
    }

    public static ContactVerificationResult RequiresUserAction(string instructionMessage, string? adminMessage = null, bool notifyAdmin = false)
    {
        return new ContactVerificationResult
        {
            IsMutualContact = false,
            RequiresManualAction = true,
            UserInstructionMessage = instructionMessage,
            AdminNotificationMessage = adminMessage,
            ShouldNotifyAdmin = notifyAdmin
        };
    }

    public static ContactVerificationResult Error(string errorMessage)
    {
        return new ContactVerificationResult
        {
            ErrorMessage = errorMessage,
            RequiresManualAction = true
        };
    }
}

