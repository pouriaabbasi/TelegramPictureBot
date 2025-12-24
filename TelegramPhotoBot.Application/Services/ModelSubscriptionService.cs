using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Application.Services;

/// <summary>
/// Service for managing model-specific subscriptions
/// </summary>
public class ModelSubscriptionService : IModelSubscriptionService
{
    private readonly IModelSubscriptionRepository _subscriptionRepository;
    private readonly IModelRepository _modelRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ModelSubscriptionService(
        IModelSubscriptionRepository subscriptionRepository,
        IModelRepository modelRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _modelRepository = modelRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ModelSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid modelId,
        TelegramStars amount,
        CancellationToken cancellationToken = default)
    {
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Verify model exists and can accept subscriptions
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        if (!model.CanAcceptSubscriptions())
        {
            throw new InvalidOperationException("Model cannot accept subscriptions at this time");
        }

        // Check if user already has an active subscription
        var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId, modelId, cancellationToken);
        if (existingSubscription != null)
        {
            throw new InvalidOperationException("User already has an active subscription to this model");
        }

        // Create subscription period
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(model.SubscriptionDurationDays!.Value);
        var period = new DateRange(startDate, endDate);

        // Create subscription
        var subscription = new ModelSubscription(userId, modelId, period, amount);
        // Note: Payment completion is handled separately by payment verification service

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        
        // Update model subscriber count
        model.IncrementSubscribers();
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription;
    }

    public async Task<IEnumerable<ModelSubscription>> GetUserSubscriptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _subscriptionRepository.GetUserSubscriptionsAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<ModelSubscription>> GetModelSubscriptionsAsync(
        Guid modelId,
        CancellationToken cancellationToken = default)
    {
        return await _subscriptionRepository.GetModelSubscriptionsAsync(modelId, cancellationToken);
    }

    public async Task<bool> HasActiveSubscriptionAsync(
        Guid userId,
        Guid modelId,
        CancellationToken cancellationToken = default)
    {
        return await _subscriptionRepository.HasActiveSubscriptionAsync(userId, modelId, cancellationToken);
    }

    public async Task<ModelSubscription?> GetSubscriptionByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
    }

    public async Task<ModelSubscription> CancelSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription not found");
        }

        subscription.DisableAutoRenew();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription;
    }

    public async Task<ModelSubscription> EnableAutoRenewAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription not found");
        }

        subscription.EnableAutoRenew();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription;
    }

    public async Task UpdateExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredSubscriptions = await _subscriptionRepository.GetExpiredActiveSubscriptionsAsync(cancellationToken);
        
        foreach (var subscription in expiredSubscriptions)
        {
            subscription.CheckAndUpdateExpiration();
            
            // Decrement model subscriber count if subscription is deactivated
            if (!subscription.IsActive)
            {
                var model = await _modelRepository.GetByIdAsync(subscription.ModelId, cancellationToken);
                if (model != null)
                {
                    model.DecrementSubscribers();
                }
            }
        }

        if (expiredSubscriptions.Any())
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

