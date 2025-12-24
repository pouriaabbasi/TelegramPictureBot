using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service responsible for delivering content via MTProto (Telegram User API)
/// </summary>
public interface IContentDeliveryService
{
    /// <summary>
    /// Validates that the recipient has the sender account in their contacts
    /// </summary>
    Task<bool> ValidateContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a photo with self-destruct timer via MTProto
    /// </summary>
    Task<ContentDeliveryResult> SendPhotoAsync(SendPhotoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a video with self-destruct timer via MTProto
    /// </summary>
    Task<ContentDeliveryResult> SendVideoAsync(SendVideoRequest request, CancellationToken cancellationToken = default);
}

