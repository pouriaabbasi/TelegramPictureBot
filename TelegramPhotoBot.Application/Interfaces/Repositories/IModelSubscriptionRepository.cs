using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

/// <summary>
/// Repository for ModelSubscription entity
/// </summary>
public interface IModelSubscriptionRepository : IRepository<ModelSubscription>
{
    /// <summary>
    /// Gets active subscriptions for a user
    /// </summary>
    Task<IEnumerable<ModelSubscription>> GetUserActiveSubscriptionsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all subscriptions for a user
    /// </summary>
    Task<IEnumerable<ModelSubscription>> GetUserSubscriptionsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all subscriptions for a specific model
    /// </summary>
    Task<IEnumerable<ModelSubscription>> GetModelSubscriptionsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user has an active subscription to a model
    /// </summary>
    Task<bool> HasActiveSubscriptionAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an active subscription for a user and model
    /// </summary>
    Task<ModelSubscription?> GetActiveSubscriptionAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all expired but still active subscriptions (needs deactivation)
    /// </summary>
    Task<IEnumerable<ModelSubscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken cancellationToken = default);
}

