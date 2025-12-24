using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Repository interface for DemoAccess tracking
/// </summary>
public interface IDemoAccessRepository : IRepository<DemoAccess>
{
    /// <summary>
    /// Check if a user has already accessed a model's demo
    /// </summary>
    Task<bool> HasUserAccessedDemoAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get demo access record for a user and model
    /// </summary>
    Task<DemoAccess?> GetDemoAccessAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Record a demo access
    /// </summary>
    Task RecordDemoAccessAsync(Guid userId, Guid modelId, string? demoFileId = null, CancellationToken cancellationToken = default);
}

