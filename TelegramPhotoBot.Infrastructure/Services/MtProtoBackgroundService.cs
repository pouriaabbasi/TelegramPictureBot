using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Application.DTOs;
using TL;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Background service for MTProto client (matching WTelegramClient working example)
/// </summary>
public sealed class MtProtoBackgroundService : BackgroundService, IMtProtoService
{
    public readonly WTelegram.Client Client;
    public User? User => Client.User;
    public string? ConfigNeeded { get; private set; } = "connecting";

    private readonly IPlatformSettingsRepository _settingsRepo;
    private readonly ILogger<MtProtoBackgroundService> _logger;

    public MtProtoBackgroundService(
        IPlatformSettingsRepository settingsRepo,
        ILogger<MtProtoBackgroundService> logger)
    {
        _settingsRepo = settingsRepo;
        _logger = logger;
        
        WTelegram.Helpers.Log = (lvl, msg) => _logger.Log((LogLevel)lvl, msg);
        
        Client = new WTelegram.Client(what =>
        {
            // Synchronous config callback - must use .Result like the working example
            var value = _settingsRepo.GetValueAsync($"telegram:mtproto:{what}", default).Result;
            Console.WriteLine($"📋 Config callback: {what} = {(what == "api_hash" ? "***" : value ?? "null")}");
            return value;
        });
    }

    public override void Dispose()
    {
        Client.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var phoneNumber = await _settingsRepo.GetValueAsync("telegram:mtproto:phone_number", stoppingToken);
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                Console.WriteLine($"🔐 Starting login with phone: {phoneNumber}");
                ConfigNeeded = await DoLogin(phoneNumber);
            }
            else
            {
                Console.WriteLine("⚠️ No phone number configured. Waiting for web setup...");
                ConfigNeeded = "api_id"; // Start from the beginning
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MTProto initialization");
            ConfigNeeded = "error";
        }
    }

    public async Task<string?> DoLogin(string loginInfo)
    {
        try
        {
            Console.WriteLine($"🔐 DoLogin called with: {loginInfo}");
            var result = await Client.Login(loginInfo);
            ConfigNeeded = result ?? "authenticated";
            Console.WriteLine($"✅ Login result: {ConfigNeeded}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Login error: {ex.Message}");
            ConfigNeeded = "error";
            throw;
        }
    }

    // IMtProtoService implementation
    public async Task<string?> LoginAsync(string loginInfo, CancellationToken cancellationToken = default)
    {
        return await DoLogin(loginInfo);
    }

    public async Task<bool> IsContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔍 Checking if user {recipientTelegramUserId} has sender in contacts...");
            
            var dialogs = await Client.Messages_GetAllDialogs();
            var user = dialogs.users.Values.OfType<User>()
                .FirstOrDefault(u => u.id == recipientTelegramUserId);

            if (user == null)
            {
                Console.WriteLine($"❌ User {recipientTelegramUserId} not found in dialogs");
                return false;
            }

            // فقط چک می‌کنیم که گیرنده، فرستنده رو در کانتکت داشته باشه
            // نیازی نیست هر دو طرف همدیگر را اد کرده باشند (mutual_contact)
            bool isContact = user.flags.HasFlag(User.Flags.contact);
            Console.WriteLine($"✅ User {recipientTelegramUserId} has sender in contacts: {isContact}");
            
            return isContact;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking contact: {ex.Message}");
            throw;
        }
    }

    public async Task<ContentDeliveryResult> SendPhotoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"📤 SendPhotoWithTimerAsync: user={recipientTelegramUserId}, file={filePath}, timer={selfDestructSeconds}s");

            // Get user from dialogs
            var dialogs = await Client.Messages_GetAllDialogs();
            var user = dialogs.users.Values.OfType<User>()
                .FirstOrDefault(u => u.id == recipientTelegramUserId);

            if (user == null)
            {
                Console.WriteLine($"❌ User {recipientTelegramUserId} not found");
                return ContentDeliveryResult.Failure("User not found");
            }

            // Upload file
            Console.WriteLine($"📤 Uploading file: {filePath}");
            var inputFile = await Client.UploadFileAsync(filePath, null);
            
            // Create media with TTL
            var media = new InputMediaUploadedPhoto
            {
                file = inputFile,
                flags = InputMediaUploadedPhoto.Flags.has_ttl_seconds,
                ttl_seconds = selfDestructSeconds
            };

            // Send media
            Console.WriteLine($"📤 Sending photo with {selfDestructSeconds}s timer...");
            var result = await Client.Messages_SendMedia(user, media, caption ?? "", DateTime.UtcNow.Ticks);

            Console.WriteLine($"✅ Photo sent successfully!");
            return ContentDeliveryResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending photo: {ex.Message}");
            return ContentDeliveryResult.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ContentDeliveryResult> SendVideoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"📤 SendVideoWithTimerAsync: user={recipientTelegramUserId}, file={filePath}, timer={selfDestructSeconds}s");

            var dialogs = await Client.Messages_GetAllDialogs();
            var user = dialogs.users.Values.OfType<User>()
                .FirstOrDefault(u => u.id == recipientTelegramUserId);

            if (user == null)
            {
                return ContentDeliveryResult.Failure("User not found");
            }

            var inputFile = await Client.UploadFileAsync(filePath, null);
            
            var media = new InputMediaUploadedDocument
            {
                file = inputFile,
                mime_type = "video/mp4",
                attributes = new[] { new DocumentAttributeVideo { duration = 0, w = 0, h = 0 } },
                flags = InputMediaUploadedDocument.Flags.has_ttl_seconds,
                ttl_seconds = selfDestructSeconds
            };

            await Client.Messages_SendMedia(user, media, caption ?? "", DateTime.UtcNow.Ticks);

            return ContentDeliveryResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending video: {ex.Message}");
            return ContentDeliveryResult.Failure($"Error: {ex.Message}");
        }
    }

    public Task ReinitializeAsync(string apiId, string apiHash, string phoneNumber, string? sessionPath = null, CancellationToken cancellationToken = default)
    {
        // Not needed for background service - just save to DB and restart app
        Console.WriteLine("⚠️ ReinitializeAsync called - restart app to apply new credentials");
        return Task.CompletedTask;
    }

    public async Task<bool> TestAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        return ConfigNeeded == null || ConfigNeeded == "authenticated";
    }
}

