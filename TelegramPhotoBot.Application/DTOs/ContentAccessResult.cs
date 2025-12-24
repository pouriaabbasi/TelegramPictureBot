namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Result of content access authorization check
/// </summary>
public class ContentAccessResult
{
    public bool HasAccess { get; init; }
    public string? Reason { get; init; }
    public ContentAccessType AccessType { get; init; }

    public static ContentAccessResult Granted(ContentAccessType accessType) => new()
    {
        HasAccess = true,
        AccessType = accessType
    };

    public static ContentAccessResult Denied(string reason) => new()
    {
        HasAccess = false,
        Reason = reason,
        AccessType = ContentAccessType.None
    };
}

public enum ContentAccessType
{
    None = 0,
    Subscription = 1,
    Purchase = 2
}

