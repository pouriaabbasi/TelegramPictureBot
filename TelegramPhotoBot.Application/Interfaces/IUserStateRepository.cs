using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Repository interface for UserState management
/// </summary>
public interface IUserStateRepository : IRepository<UserState>
{
    /// <summary>
    /// Get the current active state for a user
    /// </summary>
    Task<UserState?> GetActiveStateAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set or update user state
    /// </summary>
    Task SetStateAsync(Guid userId, UserStateType stateType, string? stateData = null, int expirationMinutes = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear user state
    /// </summary>
    Task ClearStateAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove expired states (cleanup)
    /// </summary>
    Task RemoveExpiredStatesAsync(CancellationToken cancellationToken = default);
}

