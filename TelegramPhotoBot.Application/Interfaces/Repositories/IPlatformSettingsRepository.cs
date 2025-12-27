using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface IPlatformSettingsRepository : IRepository<PlatformSettings>
{
    /// <summary>
    /// Gets a setting value by key
    /// </summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a setting entity by key
    /// </summary>
    Task<PlatformSettings?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets or updates a setting value
    /// </summary>
    Task SetValueAsync(string key, string value, string? description = null, bool isSecret = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all non-secret settings (for display)
    /// </summary>
    Task<IEnumerable<PlatformSettings>> GetNonSecretSettingsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a setting exists
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Hard deletes all MTProto-related settings (including soft-deleted ones) to ensure clean setup
    /// </summary>
    Task ClearMtProtoSettingsAsync(CancellationToken cancellationToken = default);
}

