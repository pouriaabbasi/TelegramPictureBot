using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class ModelRepository : Repository<Model>, IModelRepository
{
    public ModelRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Model?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetByStatusAsync(ModelStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.User)
            .Where(m => m.Status == status && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetApprovedModelsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.User)
            .Where(m => m.Status == ModelStatus.Approved && !m.IsDeleted)
            .OrderByDescending(m => m.TotalSubscribers)
            .ThenByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetPendingModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.User)
            .Where(m => m.Status == ModelStatus.PendingApproval && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Model>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.User)
            .Where(m => m.Status == ModelStatus.Approved 
                     && !m.IsDeleted 
                     && m.DisplayName.Contains(searchTerm))
            .OrderBy(m => m.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserAModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(m => m.UserId == userId && !m.IsDeleted, cancellationToken);
    }

    public async Task<Model?> GetWithRelatedDataAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.User)
            .Include(m => m.Photos)
            .Include(m => m.Subscriptions)
            .FirstOrDefaultAsync(m => m.Id == modelId && !m.IsDeleted, cancellationToken);
    }
}

