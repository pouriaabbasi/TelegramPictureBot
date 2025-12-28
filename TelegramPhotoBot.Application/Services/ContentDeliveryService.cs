using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;

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
        Console.WriteLine($"📤 ContentDeliveryService.SendPhotoAsync called for user {request.RecipientTelegramUserId}, photoId: {request.PhotoId}");
        
        // Validate contact before sending - catch exceptions to show error messages
        bool isContact;
        try
        {
            Console.WriteLine($"🔍 Validating contact for user {request.RecipientTelegramUserId}...");
            isContact = await ValidateContactAsync(request.RecipientTelegramUserId, cancellationToken);
            Console.WriteLine($"✅ Contact validation result: {isContact}");
        }
        catch (Exception ex)
        {
            // If there's an error checking contact, return error message
            Console.WriteLine($"❌ Exception during contact validation: {ex.Message}");
            Console.WriteLine($"❌ Exception type: {ex.GetType().FullName}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return ContentDeliveryResult.Failure($"❌ خطا در بررسی وضعیت کانتکت: {ex.Message}");
        }

        if (!isContact)
        {
            return ContentDeliveryResult.Failure("❌ لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید");
        }

        // Log view and increment view count if photoId is provided
        Photo? photoEntity = null;
        if (request.PhotoId.HasValue && request.UserId.HasValue)
        {
            photoEntity = await _photoRepository.GetByIdAsync(request.PhotoId.Value, cancellationToken);
            if (photoEntity != null)
            {
                // Log the view in history
                await _viewHistoryRepository.LogViewAsync(
                    request.UserId.Value,
                    request.PhotoId.Value,
                    photoEntity.ModelId,
                    photoEntity.Type,
                    request.ViewerUsername,
                    photoEntity.Caption,
                    cancellationToken);

                // Increment view count on the photo
                photoEntity.IncrementViewCount();
                await _photoRepository.UpdateAsync(photoEntity, cancellationToken);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        else if (request.PhotoId.HasValue)
        {
            // حتی اگر UserId نباشه، باید photo entity رو بگیریم برای MTProto caching
            photoEntity = await _photoRepository.GetByIdAsync(request.PhotoId.Value, cancellationToken);
        }

        // Send photo via MTProto with self-destruct timer
        Console.WriteLine($"📤 Calling MTProto SendPhotoWithTimerAsync for user {request.RecipientTelegramUserId}...");
        try
        {
            var result = await _mtProtoService.SendPhotoWithTimerAsync(
                request.RecipientTelegramUserId,
                request.FilePath,
                photoEntity!,
                request.Caption,
                request.SelfDestructSeconds,
                cancellationToken);
            
            if (result.IsSuccess)
            {
                Console.WriteLine($"✅ Photo sent successfully via MTProto");
                
                // Save MTProto photo info if updated
                if (photoEntity != null && photoEntity.HasMtProtoPhotoInfo())
                {
                    await _photoRepository.UpdateAsync(photoEntity, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                Console.WriteLine($"❌ Photo send failed: {result.ErrorMessage}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in SendPhotoWithTimerAsync: {ex.Message}");
            Console.WriteLine($"❌ Exception type: {ex.GetType().FullName}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return ContentDeliveryResult.Failure($"❌ خطا در ارسال عکس: {ex.Message}");
        }
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

