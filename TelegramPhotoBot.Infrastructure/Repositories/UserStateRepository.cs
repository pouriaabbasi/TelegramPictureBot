using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class UserStateRepository : Repository<UserState>, IUserStateRepository
{
    public UserStateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UserState?> GetActiveStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var state = await _dbSet
            .FirstOrDefaultAsync(us => us.UserId == userId && !us.IsDeleted, cancellationToken);

        // Return null if expired
        if (state != null && state.IsExpired())
        {
            return null;
        }

        return state;
    }

    public async Task SetStateAsync(Guid userId, UserStateType stateType, string? stateData = null, int expirationMinutes = 30, CancellationToken cancellationToken = default)
    {
        var existingState = await _dbSet
            .FirstOrDefaultAsync(us => us.UserId == userId && !us.IsDeleted, cancellationToken);

        if (existingState != null)
        {
            existingState.UpdateState(stateType, stateData, expirationMinutes);
            await UpdateAsync(existingState, cancellationToken);
        }
        else
        {
            var newState = new UserState(userId, stateType, stateData, expirationMinutes);
            await AddAsync(newState, cancellationToken);
        }
    }

    public async Task ClearStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existingState = await _dbSet
            .FirstOrDefaultAsync(us => us.UserId == userId && !us.IsDeleted, cancellationToken);

        if (existingState != null)
        {
            existingState.Clear();
            await UpdateAsync(existingState, cancellationToken);
        }
    }

    public async Task RemoveExpiredStatesAsync(CancellationToken cancellationToken = default)
    {
        var expiredStates = await _dbSet
            .Where(us => us.ExpiresAt < DateTime.UtcNow && !us.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var state in expiredStates)
        {
            state.MarkAsDeleted();
        }

        if (expiredStates.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

