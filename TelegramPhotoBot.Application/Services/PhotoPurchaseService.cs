using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Services;

public class PhotoPurchaseService : IPhotoPurchaseService
{
    private readonly IPhotoRepository _photoRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PhotoPurchaseService(
        IPhotoRepository photoRepository,
        IPurchaseRepository purchaseRepository,
        IUnitOfWork unitOfWork)
    {
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PhotoPurchaseResult> CreatePhotoPurchaseAsync(
        CreatePhotoPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get photo
        var photo = await _photoRepository.GetByIdAsync(request.PhotoId, cancellationToken);
        if (photo == null)
        {
            return PhotoPurchaseResult.Failure("Photo not found");
        }

        if (!photo.CanBePurchased())
        {
            return PhotoPurchaseResult.Failure("Photo is not available for purchase");
        }

        // Check if user already purchased this photo
        var existingPurchase = await _purchaseRepository.GetPhotoPurchaseAsync(request.UserId, request.PhotoId, cancellationToken);
        if (existingPurchase != null && existingPurchase.IsPaymentCompleted())
        {
            return PhotoPurchaseResult.Failure("You have already purchased this photo");
        }

        // Create purchase record
        var purchase = new PurchasePhoto(request.UserId, request.PhotoId, photo.Price);

        try
        {
            await _purchaseRepository.AddAsync(purchase, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return PhotoPurchaseResult.Success(purchase.Id, photo.Price.Amount);
        }
        catch (Exception ex)
        {
            return PhotoPurchaseResult.Failure($"Failed to create photo purchase: {ex.Message}");
        }
    }
}

