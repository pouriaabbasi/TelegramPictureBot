using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        // Query directly using SQL to avoid value object comparison issues
        return await _dbSet
            .FromSqlRaw("SELECT * FROM Users WHERE TelegramUserId = {0} AND IsDeleted = 0", telegramUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

