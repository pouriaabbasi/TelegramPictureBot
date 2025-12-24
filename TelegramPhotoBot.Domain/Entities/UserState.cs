using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Tracks user interaction state for multi-step workflows
/// </summary>
public class UserState : BaseEntity
{
    public Guid UserId { get; private set; }
    public UserStateType StateType { get; private set; }
    public string? StateData { get; private set; } // JSON for additional context
    public DateTime ExpiresAt { get; private set; }

    // Navigation property
    public virtual User User { get; private set; } = null!;

    // EF Core constructor
    protected UserState() { }

    public UserState(Guid userId, UserStateType stateType, string? stateData = null, int expirationMinutes = 30)
    {
        UserId = userId;
        StateType = stateType;
        StateData = stateData;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
    }

    /// <summary>
    /// Check if the state has expired
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Update the state type and data
    /// </summary>
    public void UpdateState(UserStateType stateType, string? stateData = null, int expirationMinutes = 30)
    {
        StateType = stateType;
        StateData = stateData;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        MarkAsUpdated();
    }

    /// <summary>
    /// Clear the state (set to None)
    /// </summary>
    public void Clear()
    {
        StateType = UserStateType.None;
        StateData = null;
        ExpiresAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}

