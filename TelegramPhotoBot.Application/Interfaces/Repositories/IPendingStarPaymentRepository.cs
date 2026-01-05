using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface IPendingStarPaymentRepository : IRepository<PendingStarPayment>
{
    /// <summary>
    /// Gets a pending payment by message ID and chat ID
    /// </summary>
    Task<PendingStarPayment?> GetByMessageIdAsync(long messageId, long chatId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pending payments for a user
    /// </summary>
    Task<IEnumerable<PendingStarPayment>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets expired payments that need to be marked as expired
    /// </summary>
    Task<IEnumerable<PendingStarPayment>> GetExpiredPaymentsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pending payments with a specific status
    /// </summary>
    Task<IEnumerable<PendingStarPayment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default);
}
