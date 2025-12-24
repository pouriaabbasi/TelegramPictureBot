using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service responsible for authorizing content access based on subscriptions and purchases
/// </summary>
public interface IContentAuthorizationService
{
    /// <summary>
    /// Checks if a user has access to a specific photo
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="photoId">Photo ID</param>
    /// <returns>Authorization result with access status and reason</returns>
    Task<ContentAccessResult> CheckPhotoAccessAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has an active subscription
    /// </summary>
    Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has purchased a specific photo
    /// </summary>
    Task<bool> HasPurchasedPhotoAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default);
}

