using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Services;

public class ContentDeliveryService : IContentDeliveryService
{
    private readonly IMtProtoService _mtProtoService;
    private readonly IContactVerificationService _contactVerificationService;
    private readonly IViewHistoryRepository _viewHistoryRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private const string ContactRequiredMessage = "Please add this account to your contacts first";

    public ContentDeliveryService(
        IMtProtoService mtProtoService,
        IContactVerificationService contactVerificationService,
        IViewHistoryRepository viewHistoryRepository,
        IPhotoRepository photoRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _mtProtoService = mtProtoService ?? throw new ArgumentNullException(nameof(mtProtoService));
        _contactVerificationService = contactVerificationService ?? throw new ArgumentNullException(nameof(contactVerificationService));
        _viewHistoryRepository = viewHistoryRepository ?? throw new ArgumentNullException(nameof(viewHistoryRepository));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> ValidateContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        return await _mtProtoService.IsContactAsync(recipientTelegramUserId, cancellationToken);
    }

    public async Task<ContentDeliveryResult> SendPhotoAsync(SendPhotoRequest request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"📤 ContentDeliveryService.SendPhotoAsync called for user {request.RecipientTelegramUserId}, photoId: {request.PhotoId}");
        
        // Get user entity if UserId is provided
        User? recipientUser = null;
        if (request.UserId.HasValue)
        {
            recipientUser = await _userRepository.GetByIdAsync(request.UserId.Value, cancellationToken);
        }

        if (recipientUser == null)
        {
            Console.WriteLine($"❌ User not found for userId: {request.UserId}");
            return ContentDeliveryResult.Failure("❌ کاربر یافت نشد");
        }

        // Verify and ensure mutual contact using the new service
        Console.WriteLine($"🔍 Verifying mutual contact for user {request.RecipientTelegramUserId}...");
        var verificationResult = await _contactVerificationService.VerifyAndEnsureMutualContactAsync(
            recipientUser,
            request.RecipientTelegramUserId,
            cancellationToken);

        if (verificationResult.RequiresManualAction || !verificationResult.IsMutualContact)
        {
            Console.WriteLine($"⚠️ Contact verification requires manual action or not mutual");
            
            // Return the instruction message to user
            var message = verificationResult.UserInstructionMessage 
                ?? verificationResult.ErrorMessage 
                ?? "❌ لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید";
            
            return ContentDeliveryResult.Failure(message, verificationResult);
        }

        Console.WriteLine($"✅ Mutual contact verified successfully");

        // Log view and increment view count if photoId is provided
        Photo? photoEntity = null;
        if (request.PhotoId.HasValue)
        {
            photoEntity = await _photoRepository.GetByIdAsync(request.PhotoId.Value, cancellationToken);
            if (photoEntity != null && request.UserId.HasValue)
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
        Console.WriteLine($"📤 ContentDeliveryService.SendVideoAsync called for user {request.RecipientTelegramUserId}");
        
        // Get user entity if UserId is provided
        User? recipientUser = null;
        if (request.UserId.HasValue)
        {
            recipientUser = await _userRepository.GetByIdAsync(request.UserId.Value, cancellationToken);
        }

        if (recipientUser == null)
        {
            Console.WriteLine($"❌ User not found for userId: {request.UserId}");
            return ContentDeliveryResult.Failure("❌ کاربر یافت نشد");
        }

        // Verify and ensure mutual contact using the new service
        var verificationResult = await _contactVerificationService.VerifyAndEnsureMutualContactAsync(
            recipientUser,
            request.RecipientTelegramUserId,
            cancellationToken);

        if (verificationResult.RequiresManualAction || !verificationResult.IsMutualContact)
        {
            var message = verificationResult.UserInstructionMessage 
                ?? verificationResult.ErrorMessage 
                ?? "❌ لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید";
            
            return ContentDeliveryResult.Failure(message, verificationResult);
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

