using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface IContentNotificationRepository : IRepository<ContentNotification>
{
    /// <summary>
    /// Get pending notifications (ready to be sent)
    /// </summary>
    Task<IEnumerable<ContentNotification>> GetPendingNotificationsAsync(
        int batchSize = 50, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed notifications that can be retried
    /// </summary>
    Task<IEnumerable<ContentNotification>> GetRetryableNotificationsAsync(
        int maxRetries = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for a specific content
    /// </summary>
    Task<IEnumerable<ContentNotification>> GetContentNotificationsAsync(
        Guid contentId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if notification already exists for a user and content
    /// </summary>
    Task<bool> NotificationExistsAsync(
        Guid userId, 
        Guid contentId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification statistics for a content
    /// </summary>
    Task<(int Total, int Sent, int Failed, int Pending)> GetContentNotificationStatsAsync(
        Guid contentId, 
        CancellationToken cancellationToken = default);
}
