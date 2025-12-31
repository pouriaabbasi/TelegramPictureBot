using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Services;

public class RevenueAnalyticsService : IRevenueAnalyticsService
{
    private readonly IPhotoRepository _photoRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IModelPayoutRepository _payoutRepository;
    private readonly IModelSubscriptionRepository _subscriptionRepository;

    public RevenueAnalyticsService(
        IPhotoRepository photoRepository,
        IPurchaseRepository purchaseRepository,
        IModelPayoutRepository payoutRepository,
        IModelSubscriptionRepository subscriptionRepository)
    {
        _photoRepository = photoRepository;
        _purchaseRepository = purchaseRepository;
        _payoutRepository = payoutRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<ModelRevenueDto> GetModelRevenueAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        // Get all purchases for this model's content
        var allPurchases = await _purchaseRepository.GetAllAsync(cancellationToken);
        var modelPurchases = allPurchases
            .OfType<PurchasePhoto>()
            .Where(p => p.Photo.ModelId == modelId && p.PaymentStatus == PaymentStatus.Completed)
            .ToList();

        var totalRevenue = modelPurchases.Sum(p => p.Amount.Amount);
        
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfToday = now.Date;

        var revenueThisMonth = modelPurchases
            .Where(p => p.PurchaseDate >= startOfMonth)
            .Sum(p => p.Amount.Amount);

        var revenueToday = modelPurchases
            .Where(p => p.PurchaseDate >= startOfToday)
            .Sum(p => p.Amount.Amount);

        var totalPaidOut = await _payoutRepository.GetTotalPaidAmountAsync(modelId, cancellationToken);
        var availableBalance = totalRevenue - totalPaidOut;

        var totalSubscribers = (await _subscriptionRepository.GetUserActiveSubscriptionsAsync(modelId, cancellationToken)).Count();
        var totalSales = modelPurchases.Count;

        var avgSalePrice = totalSales > 0 ? (decimal)totalRevenue / totalSales : 0;

        // Get all photos for view count
        var allPhotos = await _photoRepository.GetAllAsync(cancellationToken);
        var modelPhotos = allPhotos.Where(p => p.ModelId == modelId).ToList();
        var totalViews = modelPhotos.Sum(p => p.ViewCount);

        var conversionRate = totalViews > 0 ? (decimal)totalSales / totalViews * 100 : 0;

        var latestPayout = await _payoutRepository.GetLatestPayoutAsync(modelId, cancellationToken);
        
        // Next payout date: 1st of next month
        var nextPayoutDate = startOfMonth.AddMonths(1);

        return new ModelRevenueDto
        {
            ModelId = modelId,
            TotalRevenue = totalRevenue,
            RevenueThisMonth = revenueThisMonth,
            RevenueToday = revenueToday,
            AvailableBalance = availableBalance,
            TotalPaidOut = totalPaidOut,
            TotalSubscribers = totalSubscribers,
            TotalSales = totalSales,
            AverageSalePrice = avgSalePrice,
            ConversionRate = conversionRate,
            LastPayoutDate = latestPayout?.CompletedAt,
            NextPayoutDate = nextPayoutDate
        };
    }

    public async Task<IEnumerable<ContentStatisticsDto>> GetContentStatisticsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var allPhotos = await _photoRepository.GetAllAsync(cancellationToken);
        var modelPhotos = allPhotos.Where(p => p.ModelId == modelId && p.Type == PhotoType.Premium).ToList();

        var allPurchases = await _purchaseRepository.GetAllAsync(cancellationToken);
        var photoPurchases = allPurchases.OfType<PurchasePhoto>()
            .Where(p => p.PaymentStatus == PaymentStatus.Completed)
            .GroupBy(p => p.PhotoId)
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Revenue = g.Sum(p => p.Amount.Amount) });

        var statistics = new List<ContentStatisticsDto>();

        foreach (var photo in modelPhotos)
        {
            var purchaseData = photoPurchases.ContainsKey(photo.Id) 
                ? photoPurchases[photo.Id] 
                : new { Count = 0, Revenue = 0L };

            var conversionRate = photo.ViewCount > 0 
                ? (decimal)purchaseData.Count / photo.ViewCount * 100 
                : 0;

            statistics.Add(new ContentStatisticsDto
            {
                ContentId = photo.Id,
                ContentType = "Photo",
                Title = photo.Caption ?? "Untitled",
                ViewCount = photo.ViewCount,
                PurchaseCount = purchaseData.Count,
                TotalRevenue = purchaseData.Revenue,
                ConversionRate = conversionRate,
                Price = photo.Price.Amount,
                CreatedAt = photo.CreatedAt
            });
        }

        return statistics.OrderByDescending(s => s.TotalRevenue);
    }

    public async Task<IEnumerable<TopContentDto>> GetTopContentAsync(
        Guid modelId, 
        int topCount = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var statistics = await GetContentStatisticsAsync(modelId, cancellationToken);
        
        // Filter by date if provided
        if (startDate.HasValue || endDate.HasValue)
        {
            var start = startDate ?? DateTime.MinValue;
            var end = endDate ?? DateTime.MaxValue;
            
            statistics = statistics.Where(s => s.CreatedAt >= start && s.CreatedAt <= end);
        }

        var topContent = statistics
            .OrderByDescending(s => s.PurchaseCount)
            .ThenByDescending(s => s.TotalRevenue)
            .Take(topCount)
            .Select((s, index) => new TopContentDto
            {
                ContentId = s.ContentId,
                ContentType = s.ContentType,
                Title = s.Title,
                ViewCount = s.ViewCount,
                PurchaseCount = s.PurchaseCount,
                TotalRevenue = s.TotalRevenue,
                ConversionRate = s.ConversionRate,
                Rank = index + 1
            });

        return topContent;
    }

    public async Task<IEnumerable<PayoutHistoryDto>> GetPayoutHistoryAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var payouts = await _payoutRepository.GetModelPayoutsAsync(modelId, cancellationToken);
        
        return payouts.Select(p => new PayoutHistoryDto
        {
            PayoutId = p.Id,
            RequestedAt = p.RequestedAt,
            CompletedAt = p.CompletedAt,
            AmountStars = p.AmountStars,
            AmountFiat = p.AmountFiat,
            Currency = p.Currency,
            Method = p.Method.ToString(),
            Status = p.Status.ToString(),
            TrackingNumber = p.TrackingNumber
        }).OrderByDescending(p => p.RequestedAt);
    }

    public async Task<long> GetAvailableBalanceAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var allPurchases = await _purchaseRepository.GetAllAsync(cancellationToken);
        var totalRevenue = allPurchases
            .OfType<PurchasePhoto>()
            .Where(p => p.Photo.ModelId == modelId && p.PaymentStatus == PaymentStatus.Completed)
            .Sum(p => p.Amount.Amount);

        var totalPaidOut = await _payoutRepository.GetTotalPaidAmountAsync(modelId, cancellationToken);
        
        return totalRevenue - totalPaidOut;
    }

    public async Task<long> GetRevenueForPeriodAsync(
        Guid modelId, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        var allPurchases = await _purchaseRepository.GetAllAsync(cancellationToken);
        
        return allPurchases
            .OfType<PurchasePhoto>()
            .Where(p => p.Photo.ModelId == modelId 
                && p.PaymentStatus == PaymentStatus.Completed
                && p.PurchaseDate >= startDate 
                && p.PurchaseDate <= endDate)
            .Sum(p => p.Amount.Amount);
    }
}
