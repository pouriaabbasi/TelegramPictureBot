using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class PlatformSettingsRepository : Repository<PlatformSettings>, IPlatformSettingsRepository
{
    public PlatformSettingsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken);
        return setting?.Value;
    }

    public async Task<PlatformSettings?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PlatformSettings>()
            .Where(s => s.Key == key && !s.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SetValueAsync(string key, string value, string? description = null, bool isSecret = false, CancellationToken cancellationToken = default)
    {
        var existing = await GetByKeyAsync(key, cancellationToken);
        
        if (existing != null)
        {
            existing.UpdateValue(value);
            if (description != null)
            {
                existing.UpdateDescription(description);
            }
            // Entity is already tracked by EF Core, changes will be detected automatically
            // No need to call UpdateAsync for tracked entities
        }
        else
        {
            var newSetting = new PlatformSettings(key, value, description, isSecret);
            await AddAsync(newSetting, cancellationToken);
        }
    }

    public async Task<IEnumerable<PlatformSettings>> GetNonSecretSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<PlatformSettings>()
            .Where(s => !s.IsSecret && !s.IsDeleted)
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PlatformSettings>()
            .AnyAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);
    }
}

