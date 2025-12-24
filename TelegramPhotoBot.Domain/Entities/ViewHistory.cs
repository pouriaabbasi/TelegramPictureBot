using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Tracks all user views of content for analytics and audit purposes
/// </summary>
public class ViewHistory : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid PhotoId { get; private set; }
    public Guid ModelId { get; private set; }
    public PhotoType PhotoType { get; private set; }
    public DateTime ViewedAt { get; private set; }
    public string? ViewerUsername { get; private set; }
    public string? PhotoCaption { get; private set; }

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual Photo Photo { get; private set; } = null!;
    public virtual Model Model { get; private set; } = null!;

    // EF Core constructor
    protected ViewHistory() { }

    public ViewHistory(
        Guid userId,
        Guid photoId,
        Guid modelId,
        PhotoType photoType,
        string? viewerUsername = null,
        string? photoCaption = null)
    {
        UserId = userId;
        PhotoId = photoId;
        ModelId = modelId;
        PhotoType = photoType;
        ViewedAt = DateTime.UtcNow;
        ViewerUsername = viewerUsername;
        PhotoCaption = photoCaption;
    }

    /// <summary>
    /// Check if this view was recent (within last 24 hours)
    /// </summary>
    public bool IsRecentView() => (DateTime.UtcNow - ViewedAt).TotalHours < 24;

    /// <summary>
    /// Check if this view was within a specific time range
    /// </summary>
    public bool IsViewedBetween(DateTime start, DateTime end) 
        => ViewedAt >= start && ViewedAt <= end;
}

