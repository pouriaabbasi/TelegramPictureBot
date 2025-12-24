using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for managing photo purchases
/// </summary>
public interface IPhotoPurchaseService
{
    /// <summary>
    /// Creates a new photo purchase
    /// </summary>
    Task<PhotoPurchaseResult> CreatePhotoPurchaseAsync(
        CreatePhotoPurchaseRequest request,
        CancellationToken cancellationToken = default);
}

