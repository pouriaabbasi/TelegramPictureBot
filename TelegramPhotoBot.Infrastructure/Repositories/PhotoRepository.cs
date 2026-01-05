using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class PhotoRepository : Repository<Photo>, IPhotoRepository
{
    public PhotoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Photo>> GetAvailablePhotosAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsForSale && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContentStatisticsDto>> GetContentStatisticsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var photos = await _dbSet
            .Where(p => p.ModelId == modelId && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var statistics = new List<ContentStatisticsDto>();

        foreach (var photo in photos)
        {
            // Get purchase count for this photo
            var purchases = await _context.Purchases
                .OfType<PurchasePhoto>()
                .Where(p => p.PhotoId == photo.Id)
                .ToListAsync(cancellationToken);

            var purchaseCount = purchases.Count;
            var totalRevenue = purchases.Sum(p => p.Amount.Amount);

            var stat = new ContentStatisticsDto
            {
                ContentId = photo.Id,
                ContentType = "Photo",
                Title = photo.Caption ?? "Untitled",
                ViewCount = photo.ViewCount,
                PurchaseCount = purchaseCount,
                TotalRevenue = totalRevenue,
                ConversionRate = photo.ViewCount > 0 ? (decimal)purchaseCount / photo.ViewCount * 100 : 0,
                Price = photo.Price.Amount,
                CreatedAt = photo.CreatedAt
            };

            statistics.Add(stat);
        }

        return statistics.OrderByDescending(s => s.TotalRevenue);
    }
}

