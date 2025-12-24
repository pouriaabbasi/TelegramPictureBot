using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public abstract class Purchase : BaseEntity
{
    public Guid UserId { get; protected set; }
    public TelegramStars Amount { get; protected set; }
    public DateTime PurchaseDate { get; protected set; } = DateTime.UtcNow;
    public PaymentStatus PaymentStatus { get; protected set; } = PaymentStatus.Pending;
    
    // Telegram payment tracking fields to prevent duplicate processing
    public string? TelegramPaymentId { get; protected set; }
    public string? TelegramPreCheckoutQueryId { get; protected set; }
    public DateTime? PaymentVerifiedAt { get; protected set; }
    
    // Navigation properties
    public virtual User User { get; protected set; } = null!;

    // EF Core constructor
    protected Purchase() { }

    protected Purchase(Guid userId, TelegramStars amount)
    {
        UserId = userId;
        Amount = amount;
        PurchaseDate = DateTime.UtcNow;
        PaymentStatus = PaymentStatus.Pending;
    }

    public abstract PurchaseType GetPurchaseType();

    /// <summary>
    /// Marks the payment as completed with Telegram payment verification
    /// </summary>
    public void MarkPaymentCompleted(string telegramPaymentId, string? preCheckoutQueryId = null)
    {
        if (string.IsNullOrWhiteSpace(telegramPaymentId))
            throw new ArgumentException("Telegram payment ID cannot be null or empty", nameof(telegramPaymentId));

        if (PaymentStatus == PaymentStatus.Completed)
            throw new InvalidOperationException("Payment is already completed");

        TelegramPaymentId = telegramPaymentId;
        TelegramPreCheckoutQueryId = preCheckoutQueryId;
        PaymentStatus = PaymentStatus.Completed;
        PaymentVerifiedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Marks the payment as failed
    /// </summary>
    public void MarkPaymentFailed()
    {
        if (PaymentStatus == PaymentStatus.Completed)
            throw new InvalidOperationException("Cannot mark completed payment as failed");

        PaymentStatus = PaymentStatus.Failed;
        MarkAsUpdated();
    }

    /// <summary>
    /// Checks if payment is completed and verified
    /// </summary>
    public bool IsPaymentCompleted()
    {
        return PaymentStatus == PaymentStatus.Completed && 
               !string.IsNullOrWhiteSpace(TelegramPaymentId) &&
               PaymentVerifiedAt.HasValue;
    }
}
