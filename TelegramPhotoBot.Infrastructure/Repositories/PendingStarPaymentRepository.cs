using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class PendingStarPaymentRepository : Repository<PendingStarPayment>, IPendingStarPaymentRepository
{
    public PendingStarPaymentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PendingStarPayment?> GetByMessageIdAsync(long messageId, long chatId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.PaymentMessageId == messageId && p.ChatId == chatId, cancellationToken);
    }

    public async Task<IEnumerable<PendingStarPayment>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PendingStarPayment>> GetExpiredPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(p => p.Status == PaymentStatus.Pending && p.ExpiresAt < now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PendingStarPayment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
