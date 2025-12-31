namespace TelegramPhotoBot.Application.DTOs;

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

/// <summary>
/// Top performing content ranking
/// </summary>
public class TopContentDto
{
    public Guid ContentId { get; set; }
    public string ContentType { get; set; } = "Photo";
    public string? Title { get; set; }
    public int ViewCount { get; set; }
    public int PurchaseCount { get; set; }
    public long TotalRevenue { get; set; }
    public decimal ConversionRate { get; set; }
    public int Rank { get; set; }
}

/// <summary>
/// Payout history item
/// </summary>
public class PayoutHistoryDto
{
    public Guid PayoutId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long AmountStars { get; set; }
    public decimal AmountFiat { get; set; }
    public string Currency { get; set; } = "IRR";
    public string Method { get; set; } = "BankTransfer";
    public string Status { get; set; } = "Pending";
    public string? TrackingNumber { get; set; }
}
