using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface ICouponRepository : IRepository<Coupon>
{
    /// <summary>
    /// Gets a coupon by its code
    /// </summary>
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active coupons for a specific model
    /// </summary>
    Task<IEnumerable<Coupon>> GetModelCouponsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active admin coupons
    /// </summary>
    Task<IEnumerable<Coupon>> GetAdminCouponsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of active coupons for a model
    /// </summary>
    Task<int> GetActiveModelCouponsCountAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a coupon code already exists
    /// </summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
}
