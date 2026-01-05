namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for managing and sending batch notifications to users
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Create notifications for all subscribers of a model when new content is uploaded
    /// </summary>
    Task<int> CreateNotificationsForNewContentAsync(
        Guid modelId, 
        Guid contentId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send pending notifications in batches (respects Telegram rate limits)
    /// </summary>
    Task<(int Sent, int Failed)> SendPendingNotificationsAsync(
        int batchSize = 50, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retry failed notifications
    /// </summary>
    Task<int> RetryFailedNotificationsAsync(
        int maxRetries = 3, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification statistics for a content
    /// </summary>
    Task<(int Total, int Sent, int Failed, int Pending)> GetNotificationStatsAsync(
        Guid contentId, 
        CancellationToken cancellationToken = default);
}
