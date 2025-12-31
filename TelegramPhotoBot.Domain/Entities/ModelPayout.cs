using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Records payout transactions from platform to models
/// Tracks settlement history for revenue dashboard and legal purposes
/// </summary>
public class ModelPayout : BaseEntity
{
    /// <summary>
    /// The model receiving the payout
    /// </summary>
    public Guid ModelId { get; private set; }
    
    /// <summary>
    /// Navigation property to Model
    /// </summary>
    public virtual Model Model { get; private set; } = null!;
    
    /// <summary>
    /// Amount in Telegram Stars
    /// </summary>
    public long AmountStars { get; private set; }
    
    /// <summary>
    /// Amount in fiat currency (Toman, Dollar, etc.)
    /// </summary>
    public decimal AmountFiat { get; private set; }
    
    /// <summary>
    /// Currency code (IRR, USD, etc.)
    /// </summary>
    public string Currency { get; private set; } = null!;
    
    /// <summary>
    /// Exchange rate used (Stars to Fiat)
    /// </summary>
    public decimal ExchangeRate { get; private set; }
    
    /// <summary>
    /// Payment method used
    /// </summary>
    public PayoutMethod Method { get; private set; }
    
    /// <summary>
    /// Current status of the payout
    /// </summary>
    public PayoutStatus Status { get; private set; }
    
    /// <summary>
    /// Bank transaction reference or tracking number
    /// </summary>
    public string? TrackingNumber { get; private set; }
    
    /// <summary>
    /// Admin notes about this payout
    /// </summary>
    public string? AdminNotes { get; private set; }
    
    /// <summary>
    /// Date when payout was requested
    /// </summary>
    public DateTime RequestedAt { get; private set; }
    
    /// <summary>
    /// Date when payout was completed
    /// </summary>
    public DateTime? CompletedAt { get; private set; }
    
    /// <summary>
    /// Admin who processed this payout
    /// </summary>
    public Guid? ProcessedByAdminId { get; private set; }
    
    /// <summary>
    /// Navigation property to admin user
    /// </summary>
    public virtual User? ProcessedByAdmin { get; private set; }

    // Private constructor for EF Core
    private ModelPayout() { }

    /// <summary>
    /// Create a new payout record
    /// </summary>
    public ModelPayout(
        Guid modelId,
        long amountStars,
        decimal amountFiat,
        string currency,
        decimal exchangeRate,
        PayoutMethod method,
        Guid? processedByAdminId = null,
        string? trackingNumber = null,
        string? adminNotes = null)
    {
        if (amountStars <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amountStars));
        
        if (amountFiat <= 0)
            throw new ArgumentException("Fiat amount must be positive", nameof(amountFiat));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        
        if (exchangeRate <= 0)
            throw new ArgumentException("Exchange rate must be positive", nameof(exchangeRate));

        ModelId = modelId;
        AmountStars = amountStars;
        AmountFiat = amountFiat;
        Currency = currency;
        ExchangeRate = exchangeRate;
        Method = method;
        Status = PayoutStatus.Pending;
        RequestedAt = DateTime.UtcNow;
        ProcessedByAdminId = processedByAdminId;
        TrackingNumber = trackingNumber;
        AdminNotes = adminNotes;
    }

    /// <summary>
    /// Mark payout as processing
    /// </summary>
    public void MarkAsProcessing(Guid adminId)
    {
        if (Status != PayoutStatus.Pending)
            throw new InvalidOperationException($"Cannot process payout with status {Status}");

        Status = PayoutStatus.Processing;
        ProcessedByAdminId = adminId;
        MarkAsUpdated();
    }

    /// <summary>
    /// Mark payout as completed
    /// </summary>
    public void MarkAsCompleted(string? trackingNumber = null)
    {
        if (Status == PayoutStatus.Completed)
            throw new InvalidOperationException("Payout is already completed");

        Status = PayoutStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        
        if (!string.IsNullOrWhiteSpace(trackingNumber))
            TrackingNumber = trackingNumber;
        
        MarkAsUpdated();
    }

    /// <summary>
    /// Mark payout as failed
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        if (Status == PayoutStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed payout");

        Status = PayoutStatus.Failed;
        AdminNotes = $"{AdminNotes}\n[FAILED] {reason}".Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update tracking number
    /// </summary>
    public void UpdateTrackingNumber(string trackingNumber)
    {
        TrackingNumber = trackingNumber;
        MarkAsUpdated();
    }

    /// <summary>
    /// Add or update admin notes
    /// </summary>
    public void UpdateAdminNotes(string notes)
    {
        AdminNotes = notes;
        MarkAsUpdated();
    }
}
