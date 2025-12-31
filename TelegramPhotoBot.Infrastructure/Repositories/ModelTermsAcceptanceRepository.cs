using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class ModelTermsAcceptanceRepository : Repository<ModelTermsAcceptance>, IModelTermsAcceptanceRepository
{
    public ModelTermsAcceptanceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ModelTermsAcceptance?> GetLatestAcceptanceAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(mta => mta.ModelId == modelId && mta.IsLatestVersion)
            .OrderByDescending(mta => mta.AcceptedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ModelTermsAcceptance>> GetModelAcceptanceHistoryAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(mta => mta.ModelId == modelId)
            .OrderByDescending(mta => mta.AcceptedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasAcceptedLatestTermsAsync(Guid modelId, string latestVersion, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(mta => 
                mta.ModelId == modelId && 
                mta.TermsVersion == latestVersion && 
                mta.IsLatestVersion, 
                cancellationToken);
    }

    public async Task MarkPreviousAsOldVersionAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var previousAcceptances = await _dbSet
            .Where(mta => mta.ModelId == modelId && mta.IsLatestVersion)
            .ToListAsync(cancellationToken);

        foreach (var acceptance in previousAcceptances)
        {
            acceptance.MarkAsOldVersion();
        }
    }
}
