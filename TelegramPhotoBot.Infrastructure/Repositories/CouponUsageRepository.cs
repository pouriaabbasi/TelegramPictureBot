using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class CouponUsageRepository : Repository<CouponUsage>, ICouponUsageRepository
{
    public CouponUsageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> HasUserUsedCouponAsync(Guid userId, Guid couponId, CancellationToken cancellationToken = default)
    {
        return await _context.CouponUsages
            .AnyAsync(cu => cu.UserId == userId && cu.CouponId == couponId, cancellationToken);
    }

    public async Task<IEnumerable<CouponUsage>> GetCouponUsagesAsync(Guid couponId, CancellationToken cancellationToken = default)
    {
        return await _context.CouponUsages
            .Include(cu => cu.User)
            .Include(cu => cu.Photo)
            .Include(cu => cu.Model)
            .Where(cu => cu.CouponId == couponId)
            .OrderByDescending(cu => cu.UsedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CouponUsage>> GetModelCouponUsagesAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _context.CouponUsages
            .Include(cu => cu.User)
            .Include(cu => cu.Coupon)
            .Include(cu => cu.Photo)
            .Where(cu => cu.Coupon.ModelId == modelId)
            .OrderByDescending(cu => cu.UsedAt)
            .ToListAsync(cancellationToken);
    }
}
