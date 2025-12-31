using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class ModelPayoutRepository : Repository<ModelPayout>, IModelPayoutRepository
{
    public ModelPayoutRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ModelPayout>> GetModelPayoutsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(mp => mp.ModelId == modelId)
            .OrderByDescending(mp => mp.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ModelPayout>> GetCompletedPayoutsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(mp => mp.ModelId == modelId && mp.Status == PayoutStatus.Completed)
            .OrderByDescending(mp => mp.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ModelPayout>> GetPendingPayoutsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(mp => mp.Model)
            .Where(mp => mp.Status == PayoutStatus.Pending)
            .OrderBy(mp => mp.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetTotalPaidAmountAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(mp => mp.ModelId == modelId && mp.Status == PayoutStatus.Completed)
            .SumAsync(mp => mp.AmountStars, cancellationToken);
    }

    public async Task<ModelPayout?> GetLatestPayoutAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(mp => mp.ModelId == modelId && mp.Status == PayoutStatus.Completed)
            .OrderByDescending(mp => mp.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
