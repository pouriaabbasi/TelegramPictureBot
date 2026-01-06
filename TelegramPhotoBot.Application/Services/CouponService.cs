using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Services;

public class CouponService : ICouponService
{
    private readonly ICouponRepository _couponRepository;
    private readonly ICouponUsageRepository _couponUsageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private const int MAX_MODEL_ACTIVE_COUPONS = 5;

    public CouponService(
        ICouponRepository couponRepository,
        ICouponUsageRepository couponUsageRepository,
        IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _couponUsageRepository = couponUsageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplyCouponResult> ValidateAndApplyCouponAsync(
        ApplyCouponRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Get coupon by code
        var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, cancellationToken);
        
        if (coupon == null)
        {
            return new ApplyCouponResult
            {
                IsValid = false,
                ErrorMessageKey = "coupon.error.not_found"
            };
        }

        // Check if coupon is valid for use (active, not expired, usage limit)
        if (!coupon.IsValidForUse(DateTime.UtcNow))
        {
            return new ApplyCouponResult
            {
                IsValid = false,
                ErrorMessageKey = "coupon.error.invalid"
            };
        }

        // Check if user has already used this coupon
        var hasUsed = await _couponUsageRepository.HasUserUsedCouponAsync(
            request.UserId, 
            coupon.Id, 
            cancellationToken);
        
        if (hasUsed)
        {
            return new ApplyCouponResult
            {
                IsValid = false,
                ErrorMessageKey = "coupon.error.already_used"
            };
        }

        // Check if coupon usage type matches
        if (coupon.UsageType != request.UsageType)
        {
            return new ApplyCouponResult
            {
                IsValid = false,
                ErrorMessageKey = request.UsageType == CouponUsageType.ContentPurchase 
                    ? "coupon.error.subscription_only"
                    : "coupon.error.content_only"
            };
        }

        // For model-owned coupons, verify it applies to the correct model
        if (coupon.OwnerType == CouponOwnerType.Model)
        {
            if (request.UsageType == CouponUsageType.SubscriptionPurchase)
            {
                // For subscriptions, ModelId should match
                if (coupon.ModelId != request.ModelId)
                {
                    return new ApplyCouponResult
                    {
                        IsValid = false,
                        ErrorMessageKey = "coupon.error.wrong_model"
                    };
                }
            }
            else if (request.UsageType == CouponUsageType.ContentPurchase)
            {
                // For content purchases, verify the photo belongs to the model
                // We'll need to check this at the handler level where we have photo info
                // For now, we'll pass the ModelId from the photo
                if (coupon.ModelId != request.ModelId)
                {
                    return new ApplyCouponResult
                    {
                        IsValid = false,
                        ErrorMessageKey = "coupon.error.wrong_model"
                    };
                }
            }
        }

        // Calculate discount (50/50 split between model and platform)
        var discountAmount = (request.OriginalPriceStars * coupon.DiscountPercentage) / 100;
        var finalPrice = request.OriginalPriceStars - discountAmount;
        var modelShare = discountAmount / 2;
        var platformShare = discountAmount - modelShare; // Handle odd numbers

        return new ApplyCouponResult
        {
            IsValid = true,
            Coupon = coupon,
            DiscountAmountStars = discountAmount,
            FinalPriceStars = finalPrice,
            ModelShareStars = modelShare,
            PlatformShareStars = platformShare
        };
    }

    public async Task<Coupon> CreateCouponAsync(
        CreateCouponRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Check if code already exists
        var codeExists = await _couponRepository.CodeExistsAsync(request.Code, cancellationToken);
        if (codeExists)
        {
            throw new InvalidOperationException("Coupon code already exists");
        }

        // Check model coupon limit
        if (request.OwnerType == CouponOwnerType.Model && request.ModelId.HasValue)
        {
            var canCreate = await CanModelCreateCouponAsync(request.ModelId.Value, cancellationToken);
            if (!canCreate)
            {
                throw new InvalidOperationException($"Model has reached the maximum limit of {MAX_MODEL_ACTIVE_COUPONS} active coupons");
            }
        }

        var coupon = new Coupon(
            request.Code,
            request.DiscountPercentage,
            request.UsageType,
            request.OwnerType,
            request.ModelId,
            request.ValidFrom,
            request.ValidTo,
            request.MaxUses
        );

        await _couponRepository.AddAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coupon;
    }

    public async Task RecordCouponUsageAsync(
        Guid couponId,
        Guid userId,
        int originalPriceStars,
        int discountAmountStars,
        int finalPriceStars,
        Guid? photoId = null,
        Guid? modelId = null,
        CancellationToken cancellationToken = default)
    {
        var usage = new CouponUsage(
            couponId,
            userId,
            originalPriceStars,
            discountAmountStars,
            finalPriceStars,
            photoId,
            modelId
        );

        await _couponUsageRepository.AddAsync(usage, cancellationToken);

        // Increment coupon usage count
        var coupon = await _couponRepository.GetByIdAsync(couponId, cancellationToken);
        if (coupon != null)
        {
            coupon.IncrementUsage();
            await _couponRepository.UpdateAsync(coupon, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<CouponDto>> GetModelCouponsAsync(
        Guid modelId, 
        CancellationToken cancellationToken = default)
    {
        var coupons = await _couponRepository.GetModelCouponsAsync(modelId, cancellationToken);
        return coupons.Select(c => new CouponDto
        {
            Id = c.Id,
            Code = c.Code,
            DiscountPercentage = c.DiscountPercentage,
            UsageType = c.UsageType,
            OwnerType = c.OwnerType,
            ModelId = c.ModelId,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo,
            MaxUses = c.MaxUses,
            CurrentUses = c.CurrentUses,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        });
    }

    public async Task<IEnumerable<CouponDto>> GetAdminCouponsAsync(
        CancellationToken cancellationToken = default)
    {
        var coupons = await _couponRepository.GetAdminCouponsAsync(cancellationToken);
        return coupons.Select(c => new CouponDto
        {
            Id = c.Id,
            Code = c.Code,
            DiscountPercentage = c.DiscountPercentage,
            UsageType = c.UsageType,
            OwnerType = c.OwnerType,
            ModelId = c.ModelId,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo,
            MaxUses = c.MaxUses,
            CurrentUses = c.CurrentUses,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        });
    }

    public async Task<IEnumerable<CouponUsageDto>> GetCouponUsageStatsAsync(
        Guid couponId, 
        CancellationToken cancellationToken = default)
    {
        var usages = await _couponUsageRepository.GetCouponUsagesAsync(couponId, cancellationToken);
        return usages.Select(u => new CouponUsageDto
        {
            Id = u.Id,
            CouponCode = u.Coupon.Code,
            UserName = u.User.FirstName ?? "Unknown",
            UserTelegramId = u.User.TelegramUserId,
            UsedAt = u.UsedAt,
            OriginalPriceStars = u.OriginalPriceStars,
            DiscountAmountStars = u.DiscountAmountStars,
            FinalPriceStars = u.FinalPriceStars,
            PhotoTitle = u.Photo?.Caption,
            ModelAlias = u.Model?.Alias
        });
    }

    public async Task DeactivateCouponAsync(
        Guid couponId, 
        CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository.GetByIdAsync(couponId, cancellationToken);
        if (coupon == null)
            throw new InvalidOperationException("Coupon not found");

        coupon.Deactivate();
        await _couponRepository.UpdateAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateCouponAsync(
        Guid couponId, 
        CancellationToken cancellationToken = default)
    {
        var coupon = await _couponRepository.GetByIdAsync(couponId, cancellationToken);
        if (coupon == null)
            throw new InvalidOperationException("Coupon not found");

        coupon.Activate();
        await _couponRepository.UpdateAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CanModelCreateCouponAsync(
        Guid modelId, 
        CancellationToken cancellationToken = default)
    {
        var activeCount = await _couponRepository.GetActiveModelCouponsCountAsync(modelId, cancellationToken);
        return activeCount < MAX_MODEL_ACTIVE_COUPONS;
    }
}
