using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for managing model-specific subscriptions
/// </summary>
public interface IModelSubscriptionService
{
    /// <summary>
    /// Creates a new subscription purchase for a model
    /// </summary>
    Task<ModelSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid modelId,
        TelegramStars amount,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets active subscriptions for a user
    /// </summary>
    Task<IEnumerable<ModelSubscription>> GetUserSubscriptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all subscriptions for a specific model (model owner view)
    /// </summary>
    Task<IEnumerable<ModelSubscription>> GetModelSubscriptionsAsync(
        Guid modelId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user has an active subscription to a model
    /// </summary>
    Task<bool> HasActiveSubscriptionAsync(
        Guid userId,
        Guid modelId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific subscription by ID
    /// </summary>
    Task<ModelSubscription?> GetSubscriptionByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels a subscription (disables auto-renew)
    /// </summary>
    Task<ModelSubscription> CancelSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enables auto-renewal for a subscription
    /// </summary>
    Task<ModelSubscription> EnableAutoRenewAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks and updates expired subscriptions
    /// </summary>
    Task UpdateExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
}

