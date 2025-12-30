using TelegramPhotoBot.Application.Interfaces;

namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Result of content delivery operation
/// </summary>
public class ContentDeliveryResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? MessageId { get; init; } // Telegram message ID if successful
    public ContactVerificationResult? VerificationResult { get; init; } // Details about contact verification

    public static ContentDeliveryResult Success(string? messageId = null) => new()
    {
        IsSuccess = true,
        MessageId = messageId
    };

    public static ContentDeliveryResult Failure(string errorMessage, ContactVerificationResult? verificationResult = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        VerificationResult = verificationResult
    };
}

