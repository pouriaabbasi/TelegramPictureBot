namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Request for verifying a Telegram Stars payment
/// </summary>
public class PaymentVerificationRequest
{
    public string TelegramPaymentId { get; init; } = string.Empty;
    public string? PreCheckoutQueryId { get; init; }
    public Guid PurchaseId { get; init; }
    public long TelegramUserId { get; init; }
    public long Amount { get; init; }
    public string Currency { get; init; } = "XTR"; // Telegram Stars currency code
}

