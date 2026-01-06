using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for managing and validating coupons
/// </summary>
public interface ICouponService
{
    /// <summary>
    /// Validates and applies a coupon to a purchase
    /// </summary>
    Task<ApplyCouponResult> ValidateAndApplyCouponAsync(
        ApplyCouponRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new coupon
    /// </summary>
    Task<Coupon> CreateCouponAsync(
        CreateCouponRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records the usage of a coupon after successful payment
    /// </summary>
    Task RecordCouponUsageAsync(
        Guid couponId,
        Guid userId,
        int originalPriceStars,
        int discountAmountStars,
        int finalPriceStars,
        Guid? photoId = null,
        Guid? modelId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all coupons for a model
    /// </summary>
    Task<IEnumerable<CouponDto>> GetModelCouponsAsync(
        Guid modelId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all admin coupons
    /// </summary>
    Task<IEnumerable<CouponDto>> GetAdminCouponsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets coupon usage statistics
    /// </summary>
    Task<IEnumerable<CouponUsageDto>> GetCouponUsageStatsAsync(
        Guid couponId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deactivates a coupon
    /// </summary>
    Task DeactivateCouponAsync(
        Guid couponId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates a coupon
    /// </summary>
    Task ActivateCouponAsync(
        Guid couponId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if model can create more coupons (max 5 active)
    /// </summary>
    Task<bool> CanModelCreateCouponAsync(
        Guid modelId, 
        CancellationToken cancellationToken = default);
}
