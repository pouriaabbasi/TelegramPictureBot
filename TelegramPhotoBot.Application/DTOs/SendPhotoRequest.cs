namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Request for sending a photo via MTProto
/// </summary>
public class SendPhotoRequest
{
    public long RecipientTelegramUserId { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public int SelfDestructSeconds { get; init; } // Timer for self-destructing media
    
    // For view tracking
    public Guid? PhotoId { get; init; }
    public Guid? UserId { get; init; }
    public string? ViewerUsername { get; init; }
}

