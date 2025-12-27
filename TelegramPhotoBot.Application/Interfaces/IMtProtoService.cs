using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for interacting with Telegram User API via MTProto
/// </summary>
public interface IMtProtoService
{
    /// <summary>
    /// Gets what configuration is currently needed (null if authenticated, "verification_code", "password", etc.)
    /// </summary>
    string? ConfigNeeded { get; }
    
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

    /// <summary>
    /// Reinitializes the MTProto service with new credentials
    /// </summary>
    Task ReinitializeAsync(string apiId, string apiHash, string phoneNumber, string? sessionPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to authenticate with the current credentials
    /// </summary>
    Task<bool> TestAuthenticationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs login with the provided value (verification code, password, or phone number)
    /// Returns null if login successful, or the next required value (like "verification_code", "password")
    /// </summary>
    Task<string?> LoginAsync(string loginInfo, CancellationToken cancellationToken = default);
}

