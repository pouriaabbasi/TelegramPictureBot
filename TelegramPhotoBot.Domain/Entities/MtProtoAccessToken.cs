namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// One-time access token for MTProto web interface
/// </summary>
public class MtProtoAccessToken
{
    public Guid Token { get; set; }
    public Guid AdminUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }

    public MtProtoAccessToken()
    {
        Token = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(5);
        IsUsed = false;
    }

    public bool IsValid()
    {
        return !IsUsed && DateTime.UtcNow < ExpiresAt;
    }
}

