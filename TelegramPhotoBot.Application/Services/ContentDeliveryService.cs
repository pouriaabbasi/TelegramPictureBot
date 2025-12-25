using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;

namespace TelegramPhotoBot.Application.Services;

public class ContentDeliveryService : IContentDeliveryService
{
    private readonly IMtProtoService _mtProtoService;
    private readonly IViewHistoryRepository _viewHistoryRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private const string ContactRequiredMessage = "Please add this account to your contacts first";

    public ContentDeliveryService(
        IMtProtoService mtProtoService,
        IViewHistoryRepository viewHistoryRepository,
        IPhotoRepository photoRepository,
        IUnitOfWork unitOfWork)
    {
        _mtProtoService = mtProtoService ?? throw new ArgumentNullException(nameof(mtProtoService));
        _viewHistoryRepository = viewHistoryRepository ?? throw new ArgumentNullException(nameof(viewHistoryRepository));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> ValidateContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        return await _mtProtoService.IsContactAsync(recipientTelegramUserId, cancellationToken);
    }

    public async Task<ContentDeliveryResult> SendPhotoAsync(SendPhotoRequest request, CancellationToken cancellationToken = default)
    {
        // Validate contact before sending - catch exceptions to show error messages
        bool isContact;
        try
        {
            isContact = await ValidateContactAsync(request.RecipientTelegramUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            // If there's an error checking contact, return error message
            return ContentDeliveryResult.Failure($"❌ خطا در بررسی وضعیت کانتکت: {ex.Message}");
        }

        if (!isContact)
        {
            return ContentDeliveryResult.Failure("❌ لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید");
        }

        // Log view and increment view count if photoId is provided
        if (request.PhotoId.HasValue && request.UserId.HasValue)
        {
            var photo = await _photoRepository.GetByIdAsync(request.PhotoId.Value, cancellationToken);
            if (photo != null)
            {
                // Log the view in history
                await _viewHistoryRepository.LogViewAsync(
                    request.UserId.Value,
                    request.PhotoId.Value,
                    photo.ModelId,
                    photo.Type,
                    request.ViewerUsername,
                    photo.Caption,
                    cancellationToken);

                // Increment view count on the photo
                photo.IncrementViewCount();
                await _photoRepository.UpdateAsync(photo, cancellationToken);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        // Send photo via MTProto with self-destruct timer
        return await _mtProtoService.SendPhotoWithTimerAsync(
            request.RecipientTelegramUserId,
            request.FilePath,
            request.Caption,
            request.SelfDestructSeconds,
            cancellationToken);
    }

    public async Task<ContentDeliveryResult> SendVideoAsync(SendVideoRequest request, CancellationToken cancellationToken = default)
    {
        // Validate contact before sending - catch exceptions to show error messages
        bool isContact;
        try
        {
            isContact = await ValidateContactAsync(request.RecipientTelegramUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            // If there's an error checking contact, return error message
            return ContentDeliveryResult.Failure($"❌ خطا در بررسی وضعیت کانتکت: {ex.Message}");
        }

        if (!isContact)
        {
            return ContentDeliveryResult.Failure("❌ لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید");
        }

        // Send video via MTProto with self-destruct timer
        return await _mtProtoService.SendVideoWithTimerAsync(
            request.RecipientTelegramUserId,
            request.FilePath,
            request.Caption,
            request.SelfDestructSeconds,
            cancellationToken);
    }
}

