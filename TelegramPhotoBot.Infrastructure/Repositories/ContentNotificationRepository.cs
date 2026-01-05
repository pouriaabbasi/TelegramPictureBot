using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class ContentNotificationRepository : Repository<ContentNotification>, IContentNotificationRepository
{
    public ContentNotificationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ContentNotification>> GetPendingNotificationsAsync(
        int batchSize = 50, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cn => cn.Status == NotificationStatus.Pending)
            .OrderBy(cn => cn.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContentNotification>> GetRetryableNotificationsAsync(
        int maxRetries = 3, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cn => cn.Status == NotificationStatus.Failed && cn.RetryCount < maxRetries)
            .OrderBy(cn => cn.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContentNotification>> GetContentNotificationsAsync(
        Guid contentId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cn => cn.ContentId == contentId)
            .OrderByDescending(cn => cn.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> NotificationExistsAsync(
        Guid userId, 
        Guid contentId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(cn => cn.UserId == userId && cn.ContentId == contentId, cancellationToken);
    }

    public async Task<(int Total, int Sent, int Failed, int Pending)> GetContentNotificationStatsAsync(
        Guid contentId, 
        CancellationToken cancellationToken = default)
    {
        var notifications = await _dbSet
            .Where(cn => cn.ContentId == contentId)
            .ToListAsync(cancellationToken);

        var total = notifications.Count;
        var sent = notifications.Count(cn => cn.Status == NotificationStatus.Sent);
        var failed = notifications.Count(cn => cn.Status == NotificationStatus.Failed);
        var pending = notifications.Count(cn => cn.Status == NotificationStatus.Pending);

        return (total, sent, failed, pending);
    }
}
