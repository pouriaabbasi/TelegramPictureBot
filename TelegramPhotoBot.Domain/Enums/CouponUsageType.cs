namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Defines what type of purchases a coupon can be applied to
/// </summary>
public enum CouponUsageType
{
    /// <summary>
    /// Coupon can be used for single content purchases (photos/videos)
    /// </summary>
    ContentPurchase = 0,
    
    /// <summary>
    /// Coupon can be used for model subscription purchases
    /// </summary>
    SubscriptionPurchase = 1
}
