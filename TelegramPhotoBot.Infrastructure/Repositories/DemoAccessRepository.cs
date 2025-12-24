using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class DemoAccessRepository : Repository<DemoAccess>, IDemoAccessRepository
{
    public DemoAccessRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> HasUserAccessedDemoAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(da => da.UserId == userId && da.ModelId == modelId && !da.IsDeleted, cancellationToken);
    }

    public async Task<DemoAccess?> GetDemoAccessAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(da => da.UserId == userId && da.ModelId == modelId && !da.IsDeleted, cancellationToken);
    }

    public async Task RecordDemoAccessAsync(Guid userId, Guid modelId, string? demoFileId = null, CancellationToken cancellationToken = default)
    {
        var demoAccess = new DemoAccess(userId, modelId, demoFileId);
        await AddAsync(demoAccess, cancellationToken);
    }
}

