using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    public CouponRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpperInvariant().Trim();
        return await _context.Coupons
            .Include(c => c.Model)
            .Include(c => c.Usages)
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);
    }

    public async Task<IEnumerable<Coupon>> GetModelCouponsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .Include(c => c.Usages)
            .Where(c => c.ModelId == modelId && c.OwnerType == CouponOwnerType.Model)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Coupon>> GetAdminCouponsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .Include(c => c.Usages)
            .Where(c => c.OwnerType == CouponOwnerType.Admin)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveModelCouponsCountAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .CountAsync(c => c.ModelId == modelId 
                          && c.OwnerType == CouponOwnerType.Model 
                          && c.IsActive, 
                       cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpperInvariant().Trim();
        return await _context.Coupons
            .AnyAsync(c => c.Code == normalizedCode, cancellationToken);
    }
}
