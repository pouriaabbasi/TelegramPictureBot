using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for interacting with Telegram User API via MTProto
/// </summary>
public interface IMtProtoService
{
    /// <summary>
    /// Checks if a user has the sender account in their contacts
    /// </summary>
    Task<bool> IsContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a photo with self-destruct timer
    /// </summary>
    Task<ContentDeliveryResult> SendPhotoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a video with self-destruct timer
    /// </summary>
    Task<ContentDeliveryResult> SendVideoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default);
}

