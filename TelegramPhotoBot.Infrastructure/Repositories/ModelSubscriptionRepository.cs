using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

/// <summary>
/// Repository for ModelSubscription entity
/// </summary>
public class ModelSubscriptionRepository : Repository<ModelSubscription>, IModelSubscriptionRepository
{
    public ModelSubscriptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ModelSubscription>> GetUserActiveSubscriptionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ms => ms.UserId == userId && ms.IsActive && !ms.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ModelSubscription>> GetUserSubscriptionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ms => ms.UserId == userId && !ms.IsDeleted)
            .OrderByDescending(ms => ms.PurchaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ModelSubscription>> GetModelSubscriptionsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ms => ms.ModelId == modelId && !ms.IsDeleted)
            .OrderByDescending(ms => ms.PurchaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(ms => ms.UserId == userId && ms.ModelId == modelId && ms.IsActive && !ms.IsDeleted, cancellationToken);
    }

    public async Task<ModelSubscription?> GetActiveSubscriptionAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ms => ms.UserId == userId && ms.ModelId == modelId && ms.IsActive && !ms.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<ModelSubscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(ms => ms.IsActive && !ms.IsDeleted)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => 
            {
                return task.Result.Where(ms => ms.IsExpired());
            }, cancellationToken);
    }
}

