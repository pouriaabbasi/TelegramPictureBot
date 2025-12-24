using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Repository interface for ViewHistory tracking and analytics
/// </summary>
public interface IViewHistoryRepository : IRepository<ViewHistory>
{
    /// <summary>
    /// Log a view for analytics
    /// </summary>
    Task LogViewAsync(
        Guid userId, 
        Guid photoId, 
        Guid modelId, 
        PhotoType photoType,
        string? viewerUsername = null,
        string? photoCaption = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get total views for a photo
    /// </summary>
    Task<int> GetPhotoViewCountAsync(Guid photoId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get total views for a model's content
    /// </summary>
    Task<int> GetModelTotalViewsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user's view history
    /// </summary>
    Task<IEnumerable<ViewHistory>> GetUserViewHistoryAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get model's content views with pagination
    /// </summary>
    Task<IEnumerable<ViewHistory>> GetModelViewHistoryAsync(
        Guid modelId, 
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user has viewed a specific photo
    /// </summary>
    Task<bool> HasUserViewedPhotoAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get most viewed photos for a model
    /// </summary>
    Task<IEnumerable<(Guid PhotoId, int ViewCount)>> GetTopViewedPhotosAsync(
        Guid modelId, 
        int count = 10, 
        CancellationToken cancellationToken = default);
}

