namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Tracks individual coupon usage by users
/// </summary>
public class CouponUsage : BaseEntity
{
    /// <summary>
    /// ID of the coupon that was used
    /// </summary>
    public Guid CouponId { get; private set; }
    
    /// <summary>
    /// ID of the user who used the coupon
    /// </summary>
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// When the coupon was used
    /// </summary>
    public DateTime UsedAt { get; private set; }
    
    /// <summary>
    /// Original price in stars before discount
    /// </summary>
    public int OriginalPriceStars { get; private set; }
    
    /// <summary>
    /// Discount amount in stars
    /// </summary>
    public int DiscountAmountStars { get; private set; }
    
    /// <summary>
    /// Final price in stars after discount
    /// </summary>
    public int FinalPriceStars { get; private set; }
    
    /// <summary>
    /// Discount amount covered by the model (50%)
    /// </summary>
    public int ModelShareStars { get; private set; }
    
    /// <summary>
    /// Discount amount covered by the platform (50%)
    /// </summary>
    public int PlatformShareStars { get; private set; }
    
    /// <summary>
    /// ID of the purchased content (photo) if applicable
    /// </summary>
    public Guid? PhotoId { get; private set; }
    
    /// <summary>
    /// ID of the model if subscription purchase
    /// </summary>
    public Guid? ModelId { get; private set; }
    
    // Navigation properties
    public virtual Coupon Coupon { get; private set; } = null!;
    public virtual User User { get; private set; } = null!;
    public virtual Photo? Photo { get; private set; }
    public virtual Model? Model { get; private set; }

    // EF Core constructor
    private CouponUsage() { }

    /// <summary>
    /// Records a coupon usage
    /// </summary>
    public CouponUsage(
        Guid couponId,
        Guid userId,
        int originalPriceStars,
        int discountAmountStars,
        int finalPriceStars,
        Guid? photoId = null,
        Guid? modelId = null)
    {
        CouponId = couponId;
        UserId = userId;
        UsedAt = DateTime.UtcNow;
        OriginalPriceStars = originalPriceStars;
        DiscountAmountStars = discountAmountStars;
        FinalPriceStars = finalPriceStars;
        
        // Split discount cost 50/50 between model and platform
        ModelShareStars = discountAmountStars / 2;
        PlatformShareStars = discountAmountStars - ModelShareStars; // Handle odd numbers
        
        PhotoId = photoId;
        ModelId = modelId;
    }
}
