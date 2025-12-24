namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Request for sending a video via MTProto
/// </summary>
public class SendVideoRequest
{
    public long RecipientTelegramUserId { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public int SelfDestructSeconds { get; init; } // Timer for self-destructing media
}

