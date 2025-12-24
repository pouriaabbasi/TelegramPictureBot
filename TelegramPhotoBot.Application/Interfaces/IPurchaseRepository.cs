using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces;

public interface IPurchaseRepository : IRepository<Purchase>
{
    Task<Purchase?> GetByTelegramPaymentIdAsync(string telegramPaymentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchasePhoto>> GetPhotoPurchasesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PurchasePhoto?> GetPhotoPurchaseAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default);
}

