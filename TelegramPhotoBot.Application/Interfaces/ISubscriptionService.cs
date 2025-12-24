using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for managing subscriptions
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Creates a new subscription purchase
    /// </summary>
    Task<SubscriptionPurchaseResult> CreateSubscriptionPurchaseAsync(
        CreateSubscriptionPurchaseRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active subscription for a user
    /// </summary>
    Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
}

