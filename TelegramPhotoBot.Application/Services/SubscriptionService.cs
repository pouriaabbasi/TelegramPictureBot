using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionService(
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ISubscriptionRepository subscriptionRepository,
        IPurchaseRepository purchaseRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionPlanRepository = subscriptionPlanRepository ?? throw new ArgumentNullException(nameof(subscriptionPlanRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SubscriptionPurchaseResult> CreateSubscriptionPurchaseAsync(
        CreateSubscriptionPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get subscription plan
        var plan = await _subscriptionPlanRepository.GetByIdAsync(request.SubscriptionPlanId, cancellationToken);
        if (plan == null)
        {
            return SubscriptionPurchaseResult.Failure("Subscription plan not found");
        }

        if (!plan.IsActive)
        {
            return SubscriptionPurchaseResult.Failure("Subscription plan is not active");
        }

        // Create subscription with date range
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(plan.DurationDays);
        var period = new DateRange(startDate, endDate);
        var subscription = new Subscription(request.UserId, request.SubscriptionPlanId, period, plan.Price);

        // Create purchase record
        var purchase = new PurchaseSubscription(request.UserId, subscription.Id, plan.Price);

        try
        {
            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
            await _purchaseRepository.AddAsync(purchase, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return SubscriptionPurchaseResult.Success(purchase.Id, subscription.Id, plan.Price.Amount);
        }
        catch (Exception ex)
        {
            return SubscriptionPurchaseResult.Failure($"Failed to create subscription purchase: {ex.Message}");
        }
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId, cancellationToken);
        if (subscription == null || !subscription.IsActive())
        {
            return null;
        }

        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            PlanName = subscription.SubscriptionPlan.Name,
            StartDate = subscription.Period.StartDate,
            EndDate = subscription.Period.EndDate,
            IsActive = subscription.IsActive(),
            DaysRemaining = subscription.DaysRemaining()
        };
    }
}

