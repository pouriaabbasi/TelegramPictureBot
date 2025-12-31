using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for calculating model revenue analytics and statistics
/// </summary>
public interface IRevenueAnalyticsService
{
    /// <summary>
    /// Get comprehensive revenue analytics for a model including all metrics
    /// </summary>
    Task<RevenueAnalyticsDto> GetModelRevenueAnalyticsAsync(Guid modelId, CancellationToken cancellationToken = default);
}
