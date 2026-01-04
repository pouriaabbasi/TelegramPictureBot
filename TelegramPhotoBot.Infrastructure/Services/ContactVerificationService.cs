using Microsoft.Extensions.Logging;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Services;

public class ContactVerificationService : IContactVerificationService
{
    private readonly IUserContactVerificationRepository _verificationRepository;
    private readonly IMtProtoService _mtProtoService;
    private readonly IPlatformSettingsRepository _settingsRepository;
    private readonly ILogger<ContactVerificationService> _logger;

    public ContactVerificationService(
        IUserContactVerificationRepository verificationRepository,
        IMtProtoService mtProtoService,
        IPlatformSettingsRepository settingsRepository,
        ILogger<ContactVerificationService> logger)
    {
        _verificationRepository = verificationRepository;
        _mtProtoService = mtProtoService;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<ContactVerificationResult> VerifyAndEnsureMutualContactAsync(
        User recipientUser,
        long recipientTelegramUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting contact verification for user {UserId} (Telegram: {TelegramId})", 
                recipientUser.Id, recipientTelegramUserId);

            // Get or create verification record
            var verification = await _verificationRepository.GetByUserIdAsync(recipientUser.Id, cancellationToken);
            if (verification == null)
            {
                verification = new UserContactVerification
                {
                    UserId = recipientUser.Id,
                    User = recipientUser,
                    LastCheckedAt = DateTime.UtcNow
                };
                verification = await _verificationRepository.CreateAsync(verification, cancellationToken);
                _logger.LogInformation("Created new verification record for user {UserId}", recipientUser.Id);
            }

            // Check if we can use cached result (only if mutual contact was confirmed and checked within last 24 hours)
            var timeSinceLastCheck = DateTime.UtcNow - verification.LastCheckedAt;
            var canUseCache = verification.IsMutualContact && timeSinceLastCheck.TotalHours < 24;

            if (canUseCache)
            {
                _logger.LogInformation("✅ Using cached mutual contact status for user {UserId} (last checked {Hours} hours ago)", 
                    recipientUser.Id, timeSinceLastCheck.TotalHours.ToString("F2"));
                return ContactVerificationResult.Success();
            }

            _logger.LogInformation("🔄 Performing fresh contact check for user {UserId} (cache expired or not mutual)", 
                recipientUser.Id);

            // Check contact status with MTProto
            var contactCheckResult = await _mtProtoService.CheckDetailedContactStatusAsync(
                recipientTelegramUserId, 
                cancellationToken);

            _logger.LogInformation("Contact check result for user {UserId}: IsContact={IsContact}, IsMutual={IsMutual}, AutoAddSuccess={AutoAddSuccess}",
                recipientUser.Id, contactCheckResult.IsContact, contactCheckResult.IsMutualContact, contactCheckResult.IsAutoAddSuccessful);

            // Update verification record
            verification.IsAutoAddedToSenderContacts = contactCheckResult.IsAutoAddSuccessful;
            verification.IsMutualContact = contactCheckResult.IsMutualContact;
            verification.LastCheckedAt = DateTime.UtcNow;

            // Case 1: Mutual contact established ✅
            if (contactCheckResult.IsMutualContact)
            {
                _logger.LogInformation("✅ Mutual contact confirmed for user {UserId}", recipientUser.Id);
                await _verificationRepository.UpdateAsync(verification, cancellationToken);
                return ContactVerificationResult.Success();
            }

            // Case 2: Auto-add successful but not mutual → User needs to add sender
            if (contactCheckResult.IsAutoAddSuccessful)
            {
                _logger.LogInformation("⚠️ Auto-add successful but not mutual for user {UserId}. User needs to add sender.", 
                    recipientUser.Id);

                var senderContact = await GetSenderContactInfoAsync(cancellationToken);
                var userMessage = BuildUserInstructionMessage(senderContact, includeMessageRequest: true);

                verification.IsUserInstructedToAddContact = true;
                await _verificationRepository.UpdateAsync(verification, cancellationToken);

                return ContactVerificationResult.RequiresUserAction(userMessage);
            }

            // Case 3: Auto-add failed → Need manual intervention
            _logger.LogWarning("❌ Auto-add failed for user {UserId}. Manual intervention required.", recipientUser.Id);

            var senderInfo = await GetSenderContactInfoAsync(cancellationToken);
            var instructionMessage = BuildUserInstructionMessage(senderInfo, includeMessageRequest: true);
            var adminMessage = BuildAdminNotificationMessage(recipientUser, recipientTelegramUserId, senderInfo);

            verification.IsUserInstructedToAddContact = true;
            verification.IsAdminNotified = true;
            verification.LastErrorMessage = "Auto-add to sender contacts failed";
            await _verificationRepository.UpdateAsync(verification, cancellationToken);

            return ContactVerificationResult.RequiresUserAction(
                instructionMessage, 
                adminMessage, 
                notifyAdmin: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during contact verification for user {UserId}", recipientUser.Id);
            return ContactVerificationResult.Error($"خطا در بررسی وضعیت کانتکت: {ex.Message}");
        }
    }

    public async Task MarkUserSentMessageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var verification = await _verificationRepository.GetByUserIdAsync(userId, cancellationToken);
        if (verification != null && !verification.HasUserSentMessage)
        {
            verification.HasUserSentMessage = true;
            await _verificationRepository.UpdateAsync(verification, cancellationToken);
            _logger.LogInformation("Marked user {UserId} as having sent a message", userId);
        }
    }

    public async Task<string?> GetSenderContactInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get sender's username from MTProto service
            var username = await _mtProtoService.GetAuthenticatedUsernameAsync(cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(username))
            {
                return username; // Already formatted as @username
            }
            
            _logger.LogWarning("Sender does not have a username");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sender contact info");
            return null;
        }
    }

    private string BuildUserInstructionMessage(string? senderContact, bool includeMessageRequest)
    {
        var message = "📱 برای دریافت محتوا، لطفاً مراحل زیر را به ترتیب انجام دهید:\n\n";

        if (!string.IsNullOrWhiteSpace(senderContact))
        {
            message += $"۱. روی لینک زیر کلیک کنید و دکمه «Add to Contacts» را بزنید:\n";
            message += $"👉 {senderContact}\n\n";
        }
        else
        {
            message += $"۱. اکانت فرستنده را به لیست مخاطبین خود اضافه کنید\n";
            message += $"   (اطلاعات تماس از طریق ادمین دریافت خواهید کرد)\n\n";
        }

        if (includeMessageRequest)
        {
            message += $"۲. پس از اضافه کردن، یک پیام کوتاه برای ما ارسال کنید\n";
            message += $"   (مثلاً: \"سلام\" یا \"آماده‌ام\")\n\n";
        }

        message += $"⚠️ این مراحل برای امنیت شما و تضمین دریافت محتوا ضروری است.\n";
        message += $"💡 تا زمانی که این مراحل انجام نشود، امکان ارسال محتوا وجود ندارد.";

        return message;
    }

    private string BuildAdminNotificationMessage(User user, long telegramUserId, string? senderContact)
    {
        var message = "🔔 <b>درخواست اضافه کردن کانتکت دستی</b>\n\n";
        message += $"👤 کاربر: {user.FirstName}";
        
        if (!string.IsNullOrWhiteSpace(user.Username))
        {
            message += $" (@{user.Username})";
        }
        
        message += $"\n🆔 User ID: <code>{user.Id}</code>";
        message += $"\n📱 Telegram ID: <code>{telegramUserId}</code>\n\n";
        
        message += $"⚠️ <b>اضافه شدن خودکار به کانتکت‌ها موفق نبود!</b>\n\n";
        message += $"📋 <b>اقدامات لازم:</b>\n";
        message += $"۱. از اپلیکیشن اصلی تلگرام، کاربر بالا را به کانتکت‌های اکانت فرستنده اضافه کنید\n";
        
        if (!string.IsNullOrWhiteSpace(senderContact))
        {
            message += $"۲. به کاربر اطلاع داده شده که {senderContact} را به کانتکت‌هایش اضافه کند\n";
        }
        else
        {
            message += $"۲. اطلاعات تماس فرستنده را به کاربر ارسال کنید\n";
        }
        
        message += $"۳. از کاربر بخواهید یک پیام ارسال کند تا Mutual Contact برقرار شود\n\n";
        message += $"💡 پس از انجام این مراحل، ارسال محتوا امکان‌پذیر خواهد بود.";

        return message;
    }
}

