namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Request for creating a Telegram Stars invoice
/// </summary>
public class CreateInvoiceRequest
{
    public long ChatId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty; // Unique identifier for the purchase
    public string ProviderToken { get; init; } = string.Empty; // Usually empty for Telegram Stars
    public string Currency { get; init; } = "XTR"; // Telegram Stars
    public long Amount { get; init; } // Amount in stars
    public Dictionary<string, string>? Prices { get; init; } // Price breakdown
}

