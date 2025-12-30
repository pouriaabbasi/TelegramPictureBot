using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface IUserContactVerificationRepository
{
    Task<UserContactVerification?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserContactVerification> CreateAsync(UserContactVerification verification, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserContactVerification verification, CancellationToken cancellationToken = default);
    Task<List<UserContactVerification>> GetPendingManualAddsAsync(CancellationToken cancellationToken = default);
}

