using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;

namespace TelegramPhotoBot.Application.Services;

public class ContentAuthorizationService : IContentAuthorizationService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IModelSubscriptionService _modelSubscriptionService;
    private readonly IPhotoRepository _photoRepository;

    public ContentAuthorizationService(
        IPurchaseRepository purchaseRepository,
        IModelSubscriptionService modelSubscriptionService,
        IPhotoRepository photoRepository)
    {
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _modelSubscriptionService = modelSubscriptionService ?? throw new ArgumentNullException(nameof(modelSubscriptionService));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
    }

    public async Task<ContentAccessResult> CheckPhotoAccessAsync(
        Guid userId, 
        Guid photoId, 
        CancellationToken cancellationToken = default)
    {
        // Get the photo to determine which model it belongs to
        var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
        if (photo == null)
        {
            return ContentAccessResult.Denied("Photo not found.");
        }

        // Check if user has active subscription to this model
        var hasModelSubscription = await _modelSubscriptionService.HasActiveSubscriptionAsync(userId, photo.ModelId, cancellationToken);
        if (hasModelSubscription)
        {
            return ContentAccessResult.Granted(ContentAccessType.Subscription);
        }

        // Check if user has purchased this specific photo
        var hasPurchased = await HasPurchasedPhotoAsync(userId, photoId, cancellationToken);
        if (hasPurchased)
        {
            return ContentAccessResult.Granted(ContentAccessType.Purchase);
        }

        return ContentAccessResult.Denied("You need an active subscription or purchase this photo to access it.");
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Platform-wide subscriptions are no longer supported
        // All subscriptions are now model-specific
        return false;
    }

    public async Task<bool> HasPurchasedPhotoAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetPhotoPurchaseAsync(userId, photoId, cancellationToken);
        return purchase != null && purchase.IsPaymentCompleted();
    }
}

