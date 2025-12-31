using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for calculating model revenue analytics and statistics
/// </summary>
public interface IRevenueAnalyticsService
{
    /// <summary>
    /// Get complete revenue overview for a model
    /// </summary>
    Task<ModelRevenueDto> GetModelRevenueAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get statistics for all content items of a model
    /// </summary>
    Task<IEnumerable<ContentStatisticsDto>> GetContentStatisticsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get top performing content (by purchases, views, or revenue)
    /// </summary>
    Task<IEnumerable<TopContentDto>> GetTopContentAsync(
        Guid modelId, 
        int topCount = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get payout history for a model
    /// </summary>
    Task<IEnumerable<PayoutHistoryDto>> GetPayoutHistoryAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate available balance (earned but not paid out)
    /// </summary>
    Task<long> GetAvailableBalanceAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get revenue for a specific time period
    /// </summary>
    Task<long> GetRevenueForPeriodAsync(
        Guid modelId, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
}
