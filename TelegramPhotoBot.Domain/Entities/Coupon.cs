using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Represents a discount coupon that can be applied to purchases
/// </summary>
public class Coupon : BaseEntity
{
    /// <summary>
    /// Unique coupon code entered by users
    /// </summary>
    public string Code { get; private set; }
    
    /// <summary>
    /// Discount percentage (0-100)
    /// </summary>
    public int DiscountPercentage { get; private set; }
    
    /// <summary>
    /// Type of purchase this coupon applies to
    /// </summary>
    public CouponUsageType UsageType { get; private set; }
    
    /// <summary>
    /// Who created this coupon
    /// </summary>
    public CouponOwnerType OwnerType { get; private set; }
    
    /// <summary>
    /// Model ID if OwnerType is Model, null for Admin coupons
    /// </summary>
    public Guid? ModelId { get; private set; }
    
    /// <summary>
    /// Optional start date for coupon validity
    /// </summary>
    public DateTime? ValidFrom { get; private set; }
    
    /// <summary>
    /// Optional end date for coupon validity
    /// </summary>
    public DateTime? ValidTo { get; private set; }
    
    /// <summary>
    /// Maximum total uses across all users (null = unlimited)
    /// </summary>
    public int? MaxUses { get; private set; }
    
    /// <summary>
    /// Current number of times this coupon has been used
    /// </summary>
    public int CurrentUses { get; private set; }
    
    /// <summary>
    /// Whether the coupon is active
    /// </summary>
    public bool IsActive { get; private set; }
    
    // Navigation properties
    public virtual Model? Model { get; private set; }
    public virtual ICollection<CouponUsage> Usages { get; private set; } = new List<CouponUsage>();

    // EF Core constructor
    private Coupon() { }

    /// <summary>
    /// Creates a new coupon
    /// </summary>
    public Coupon(
        string code,
        int discountPercentage,
        CouponUsageType usageType,
        CouponOwnerType ownerType,
        Guid? modelId = null,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        int? maxUses = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Coupon code cannot be empty", nameof(code));
        
        if (discountPercentage < 1 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 1 and 100", nameof(discountPercentage));
        
        if (ownerType == CouponOwnerType.Model && !modelId.HasValue)
            throw new ArgumentException("ModelId is required for Model-owned coupons", nameof(modelId));
        
        if (ownerType == CouponOwnerType.Admin && modelId.HasValue)
            throw new ArgumentException("ModelId should be null for Admin-owned coupons", nameof(modelId));
        
        if (validFrom.HasValue && validTo.HasValue && validFrom.Value > validTo.Value)
            throw new ArgumentException("ValidFrom cannot be after ValidTo");
        
        if (maxUses.HasValue && maxUses.Value < 1)
            throw new ArgumentException("MaxUses must be at least 1 if specified", nameof(maxUses));

        Code = code.ToUpperInvariant().Trim();
        DiscountPercentage = discountPercentage;
        UsageType = usageType;
        OwnerType = ownerType;
        ModelId = modelId;
        ValidFrom = validFrom;
        ValidTo = validTo;
        MaxUses = maxUses;
        CurrentUses = 0;
        IsActive = true;
    }

    /// <summary>
    /// Checks if the coupon is valid for use
    /// </summary>
    public bool IsValidForUse(DateTime now)
    {
        if (!IsActive)
            return false;

        if (ValidFrom.HasValue && now < ValidFrom.Value)
            return false;

        if (ValidTo.HasValue && now > ValidTo.Value)
            return false;

        if (MaxUses.HasValue && CurrentUses >= MaxUses.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Increments the usage count
    /// </summary>
    public void IncrementUsage()
    {
        CurrentUses++;
        MarkAsUpdated();
    }

    /// <summary>
    /// Deactivates the coupon
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Reactivates the coupon
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    /// <summary>
    /// Updates coupon properties
    /// </summary>
    public void Update(
        int? discountPercentage = null,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        int? maxUses = null)
    {
        if (discountPercentage.HasValue)
        {
            if (discountPercentage.Value < 1 || discountPercentage.Value > 100)
                throw new ArgumentException("Discount percentage must be between 1 and 100");
            DiscountPercentage = discountPercentage.Value;
        }

        if (validFrom.HasValue || validTo.HasValue)
        {
            var newFrom = validFrom ?? ValidFrom;
            var newTo = validTo ?? ValidTo;
            
            if (newFrom.HasValue && newTo.HasValue && newFrom.Value > newTo.Value)
                throw new ArgumentException("ValidFrom cannot be after ValidTo");
            
            ValidFrom = newFrom;
            ValidTo = newTo;
        }

        if (maxUses.HasValue)
        {
            if (maxUses.Value < 1)
                throw new ArgumentException("MaxUses must be at least 1 if specified");
            MaxUses = maxUses.Value;
        }

        MarkAsUpdated();
    }
}
