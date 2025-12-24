namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Tracks demo content access - ensures one-time viewing per user
/// </summary>
public class DemoAccess : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ModelId { get; private set; }
    public DateTime AccessedAt { get; private set; }
    public string? DemoFileId { get; private set; } // Reference to the demo content

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual Model Model { get; private set; } = null!;

    // EF Core constructor
    protected DemoAccess() { }

    public DemoAccess(Guid userId, Guid modelId, string? demoFileId = null)
    {
        UserId = userId;
        ModelId = modelId;
        AccessedAt = DateTime.UtcNow;
        DemoFileId = demoFileId;
    }

    /// <summary>
    /// Check if the access is still recent (within last 24 hours for debugging)
    /// </summary>
    public bool IsRecentAccess() => (DateTime.UtcNow - AccessedAt).TotalHours < 24;
}

