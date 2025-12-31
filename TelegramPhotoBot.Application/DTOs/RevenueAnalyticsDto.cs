namespace TelegramPhotoBot.Application.DTOs;

public class RevenueAnalyticsDto
{
    public decimal TotalRevenueStars { get; set; }
    public decimal ThisMonthRevenueStars { get; set; }
    public decimal TodayRevenueStars { get; set; }
    public decimal AvailableBalanceStars { get; set; }
    public int TotalSubscribers { get; set; }
    public int TotalSales { get; set; }
    public decimal AverageSalePriceStars { get; set; }
    public decimal ConversionRate { get; set; } // Purchases / Views

    public List<ContentStatsDto> ContentStatistics { get; set; } = new();
    public List<TopContentDto> TopMonthlyContent { get; set; } = new();
    public List<TopContentDto> TopYearlyContent { get; set; } = new();
    public List<TopContentDto> TopOverallContent { get; set; } = new();
    public List<PayoutHistoryDto> PayoutHistory { get; set; } = new();
}

public class ContentStatsDto
{
    public Guid ContentId { get; set; }
    public string ContentName { get; set; } = string.Empty;
    public int Views { get; set; }
    public int Purchases { get; set; }
    public decimal RevenueStars { get; set; }
    public decimal ConversionRate { get; set; }
}

public class TopContentDto
{
    public Guid ContentId { get; set; }
    public string ContentName { get; set; } = string.Empty;
    public int MetricValue { get; set; } // e.g., number of sales or views
    public string MetricType { get; set; } = string.Empty; // e.g., "Sales", "Views"
}

public class PayoutHistoryDto
{
    public Guid PayoutId { get; set; }
    public DateTime PayoutDate { get; set; }
    public decimal AmountStars { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Revenue overview for a model
/// </summary>
public class ModelRevenueDto
{
    public Guid ModelId { get; set; }
    public long TotalRevenue { get; set; } // Total Stars earned
    public long RevenueThisMonth { get; set; }
    public long RevenueToday { get; set; }
    public long AvailableBalance { get; set; } // Not yet paid out
    public long TotalPaidOut { get; set; }
    public int TotalSubscribers { get; set; }
    public int TotalSales { get; set; }
    public decimal AverageSalePrice { get; set; }
    public decimal ConversionRate { get; set; } // Sales / Views
    public DateTime? LastPayoutDate { get; set; }
    public DateTime? NextPayoutDate { get; set; }
}

/// <summary>
/// Content statistics for a single photo/video
/// </summary>
public class ContentStatisticsDto
{
    public Guid ContentId { get; set; }
    public string ContentType { get; set; } = "Photo"; // Photo or Video
    public string? Title { get; set; }
    public int ViewCount { get; set; }
    public int PurchaseCount { get; set; }
    public long TotalRevenue { get; set; } // Stars
    public decimal ConversionRate { get; set; } // Purchase / View ratio
    public long Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
