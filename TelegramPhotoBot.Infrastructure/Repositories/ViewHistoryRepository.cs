using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class ViewHistoryRepository : Repository<ViewHistory>, IViewHistoryRepository
{
    public ViewHistoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task LogViewAsync(
        Guid userId, 
        Guid photoId, 
        Guid modelId, 
        PhotoType photoType, 
        string? viewerUsername = null, 
        string? photoCaption = null, 
        CancellationToken cancellationToken = default)
    {
        var viewHistory = new ViewHistory(
            userId, 
            photoId, 
            modelId, 
            photoType, 
            viewerUsername, 
            photoCaption);
        
        await AddAsync(viewHistory, cancellationToken);
    }

    public async Task<int> GetPhotoViewCountAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(vh => vh.PhotoId == photoId && !vh.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetModelTotalViewsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(vh => vh.ModelId == modelId && !vh.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<ViewHistory>> GetUserViewHistoryAsync(
        Guid userId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(vh => vh.UserId == userId && !vh.IsDeleted)
            .OrderByDescending(vh => vh.ViewedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ViewHistory>> GetModelViewHistoryAsync(
        Guid modelId, 
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(vh => vh.ModelId == modelId && !vh.IsDeleted);

        if (startDate.HasValue)
        {
            query = query.Where(vh => vh.ViewedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(vh => vh.ViewedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(vh => vh.ViewedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasUserViewedPhotoAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(vh => vh.UserId == userId && vh.PhotoId == photoId && !vh.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<(Guid PhotoId, int ViewCount)>> GetTopViewedPhotosAsync(
        Guid modelId, 
        int count = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(vh => vh.ModelId == modelId && !vh.IsDeleted)
            .GroupBy(vh => vh.PhotoId)
            .Select(g => new { PhotoId = g.Key, ViewCount = g.Count() })
            .OrderByDescending(x => x.ViewCount)
            .Take(count)
            .Select(x => ValueTuple.Create(x.PhotoId, x.ViewCount))
            .ToListAsync(cancellationToken);
    }
}

