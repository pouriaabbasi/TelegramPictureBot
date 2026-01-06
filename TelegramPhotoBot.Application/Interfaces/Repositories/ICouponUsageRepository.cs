using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface ICouponUsageRepository : IRepository<CouponUsage>
{
    /// <summary>
    /// Checks if a user has already used a specific coupon
    /// </summary>
    Task<bool> HasUserUsedCouponAsync(Guid userId, Guid couponId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all usages for a specific coupon
    /// </summary>
    Task<IEnumerable<CouponUsage>> GetCouponUsagesAsync(Guid couponId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets coupon usage statistics for a model
    /// </summary>
    Task<IEnumerable<CouponUsage>> GetModelCouponUsagesAsync(Guid modelId, CancellationToken cancellationToken = default);
}
