namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Defines who created and owns the coupon
/// </summary>
public enum CouponOwnerType
{
    /// <summary>
    /// Coupon created by platform admin - applies to all content
    /// </summary>
    Admin = 0,
    
    /// <summary>
    /// Coupon created by a model - applies only to their content
    /// </summary>
    Model = 1
}
