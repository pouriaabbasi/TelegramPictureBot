using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Request to validate and apply a coupon
/// </summary>
public class ApplyCouponRequest
{
    public string CouponCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public int OriginalPriceStars { get; set; }
    public CouponUsageType UsageType { get; set; }
    public Guid? PhotoId { get; set; }
    public Guid? ModelId { get; set; }
}

/// <summary>
/// Result of coupon validation and application
/// </summary>
public class ApplyCouponResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessageKey { get; set; }
    public Coupon? Coupon { get; set; }
    public int DiscountAmountStars { get; set; }
    public int FinalPriceStars { get; set; }
    public int ModelShareStars { get; set; }
    public int PlatformShareStars { get; set; }
}

/// <summary>
/// Request to create a new coupon
/// </summary>
public class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public int DiscountPercentage { get; set; }
    public CouponUsageType UsageType { get; set; }
    public CouponOwnerType OwnerType { get; set; }
    public Guid? ModelId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int? MaxUses { get; set; }
}

/// <summary>
/// DTO for displaying coupon information
/// </summary>
public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int DiscountPercentage { get; set; }
    public CouponUsageType UsageType { get; set; }
    public CouponOwnerType OwnerType { get; set; }
    public Guid? ModelId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public int CurrentUses { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for coupon usage statistics
/// </summary>
public class CouponUsageDto
{
    public Guid Id { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public long UserTelegramId { get; set; }
    public DateTime UsedAt { get; set; }
    public int OriginalPriceStars { get; set; }
    public int DiscountAmountStars { get; set; }
    public int FinalPriceStars { get; set; }
    public string? PhotoTitle { get; set; }
    public string? ModelAlias { get; set; }
}
