using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class PurchaseRepository : Repository<Purchase>, IPurchaseRepository
{
    public PurchaseRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Purchase?> GetByTelegramPaymentIdAsync(string telegramPaymentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.TelegramPaymentId == telegramPaymentId, cancellationToken);
    }

    public async Task<IEnumerable<PurchasePhoto>> GetPhotoPurchasesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PurchasePhoto>()
            .Include(p => p.Photo)
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PurchasePhoto?> GetPhotoPurchaseAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PurchasePhoto>()
            .Include(p => p.Photo)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.PhotoId == photoId, cancellationToken);
    }
}

