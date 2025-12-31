using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramPhotoBot.Application.Services;

public class RevenueAnalyticsService : IRevenueAnalyticsService
{
    private readonly IModelRepository _modelRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IModelSubscriptionRepository _modelSubscriptionRepository;
    private readonly IModelPayoutRepository _modelPayoutRepository;

    public RevenueAnalyticsService(
        IModelRepository modelRepository,
        IPhotoRepository photoRepository,
        IPurchaseRepository purchaseRepository,
        IModelSubscriptionRepository modelSubscriptionRepository,
        IModelPayoutRepository modelPayoutRepository)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _modelSubscriptionRepository = modelSubscriptionRepository ?? throw new ArgumentNullException(nameof(modelSubscriptionRepository));
        _modelPayoutRepository = modelPayoutRepository ?? throw new ArgumentNullException(nameof(modelPayoutRepository));
    }

    public async Task<RevenueAnalyticsDto> GetModelRevenueAnalyticsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new ArgumentException($"Model with ID {modelId} not found.");
        }

        var allPhotos = await _photoRepository.GetAllAsync(cancellationToken);
        var photos = allPhotos.Where(p => p.ModelId == modelId).ToList();
        
        var allPurchases = await _purchaseRepository.GetAllAsync(cancellationToken);
        var purchases = allPurchases
            .Where(p => p is PurchasePhoto pp && pp.Photo.ModelId == modelId)
            .ToList();
        
        var subscriptions = (await _modelSubscriptionRepository.GetModelSubscriptionsAsync(modelId, cancellationToken)).ToList();
        var payouts = (await _modelPayoutRepository.GetModelPayoutsAsync(modelId, cancellationToken)).ToList();

        var totalRevenue = purchases.Sum(p => p.Amount.Amount) + subscriptions.Sum(s => s.Amount.Amount);
        var thisMonthRevenue = purchases.Where(p => p.PurchaseDate.Month == DateTime.UtcNow.Month && p.PurchaseDate.Year == DateTime.UtcNow.Year).Sum(p => p.Amount.Amount) +
                               subscriptions.Where(s => s.PurchaseDate.Month == DateTime.UtcNow.Month && s.PurchaseDate.Year == DateTime.UtcNow.Year).Sum(s => s.Amount.Amount);
        var todayRevenue = purchases.Where(p => p.PurchaseDate.Date == DateTime.UtcNow.Date).Sum(p => p.Amount.Amount) +
                           subscriptions.Where(s => s.PurchaseDate.Date == DateTime.UtcNow.Date).Sum(s => s.Amount.Amount);

        var totalPayouts = payouts.Where(p => p.Status == PayoutStatus.Completed).Sum(p => p.AmountStars);
        var availableBalance = totalRevenue - totalPayouts;

        var totalViews = photos.Sum(p => p.ViewCount);
        var totalPurchases = purchases.Count;
        var conversionRate = totalViews > 0 ? (decimal)totalPurchases / totalViews * 100 : 0;

        var contentStats = photos.Select(p =>
        {
            var contentPurchases = purchases.Where(pr => pr is PurchasePhoto pp && pp.PhotoId == p.Id).ToList();
            var contentRevenue = contentPurchases.Sum(pr => pr.Amount.Amount);
            var contentConversionRate = p.ViewCount > 0 ? (decimal)contentPurchases.Count / p.ViewCount * 100 : 0;

            return new ContentStatsDto
            {
                ContentId = p.Id,
                ContentName = p.Caption ?? $"Photo {p.Id.ToString().Substring(0, 8)}",
                Views = p.ViewCount,
                Purchases = contentPurchases.Count,
                RevenueStars = contentRevenue,
                ConversionRate = contentConversionRate
            };
        }).ToList();

        var topMonthlyContent = contentStats
            .Where(c => purchases.Any(p => p is PurchasePhoto pp && pp.PhotoId == c.ContentId && p.PurchaseDate.Month == DateTime.UtcNow.Month && p.PurchaseDate.Year == DateTime.UtcNow.Year))
            .OrderByDescending(c => c.Purchases)
            .Take(10)
            .Select(c => new TopContentDto
            {
                ContentId = c.ContentId,
                ContentName = c.ContentName,
                MetricValue = c.Purchases,
                MetricType = "Sales"
            }).ToList();

        var topYearlyContent = contentStats
            .Where(c => purchases.Any(p => p is PurchasePhoto pp && pp.PhotoId == c.ContentId && p.PurchaseDate.Year == DateTime.UtcNow.Year))
            .OrderByDescending(c => c.Purchases)
            .Take(10)
            .Select(c => new TopContentDto
            {
                ContentId = c.ContentId,
                ContentName = c.ContentName,
                MetricValue = c.Purchases,
                MetricType = "Sales"
            }).ToList();

        var topOverallContent = contentStats
            .OrderByDescending(c => c.Purchases)
            .Take(10)
            .Select(c => new TopContentDto
            {
                ContentId = c.ContentId,
                ContentName = c.ContentName,
                MetricValue = c.Purchases,
                MetricType = "Sales"
            }).ToList();

        var payoutHistory = payouts.Select(p => new PayoutHistoryDto
        {
            PayoutId = p.Id,
            PayoutDate = p.CompletedAt ?? p.RequestedAt,
            AmountStars = p.AmountStars,
            Method = p.Method.ToString(),
            Status = p.Status.ToString(),
            TransactionReference = p.TrackingNumber,
            Notes = p.AdminNotes
        }).OrderByDescending(p => p.PayoutDate).ToList();

        return new RevenueAnalyticsDto
        {
            TotalRevenueStars = totalRevenue,
            ThisMonthRevenueStars = thisMonthRevenue,
            TodayRevenueStars = todayRevenue,
            AvailableBalanceStars = availableBalance,
            TotalSubscribers = subscriptions.Count(s => s.IsActive),
            TotalSales = totalPurchases + subscriptions.Count,
            AverageSalePriceStars = totalPurchases > 0 ? purchases.Sum(p => p.Amount.Amount) / totalPurchases : 0,
            ConversionRate = conversionRate,
            ContentStatistics = contentStats,
            TopMonthlyContent = topMonthlyContent,
            TopYearlyContent = topYearlyContent,
            TopOverallContent = topOverallContent,
            PayoutHistory = payoutHistory
        };
    }
}
