namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Payment status for Telegram Stars payments
/// </summary>
public enum PaymentStatus
{
    Pending = 1,      // Payment initiated but not confirmed
    Completed = 2,    // Payment successfully verified and completed
    Failed = 3,        // Payment failed or was rejected
    Refunded = 4      // Payment was refunded (if applicable)
}

