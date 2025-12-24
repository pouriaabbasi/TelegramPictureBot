using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class SubscriptionPlanRepository : Repository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sp => sp.IsActive && !sp.IsDeleted)
            .OrderBy(sp => sp.Price.Amount)
            .ToListAsync(cancellationToken);
    }
}

