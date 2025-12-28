using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Application.DTOs;
using TL;
using Telegram.Bot;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Background service for MTProto client (matching WTelegramClient working example)
/// </summary>
public sealed class MtProtoBackgroundService : BackgroundService, IMtProtoService
{
    private readonly SemaphoreSlim _authLock = new SemaphoreSlim(1, 1);
    private bool _isAuthenticated = false;

    public readonly WTelegram.Client Client;
    public User? User => Client.User;
    public string? ConfigNeeded { get; set; } = "connecting";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MtProtoBackgroundService> _logger;
    private readonly ITelegramBotClient _botClient;

    public MtProtoBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MtProtoBackgroundService> logger,
        ITelegramBotClient botClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botClient = botClient;
        
        WTelegram.Helpers.Log = (lvl, msg) => _logger.Log((LogLevel)lvl, msg);
        
        Client = new WTelegram.Client(what =>
        {
            // Synchronous config callback - must use .Result like the working example
            using var scope = _serviceProvider.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<IPlatformSettingsRepository>();
            var value = settingsRepo.GetValueAsync($"telegram:mtproto:{what}", default).Result;
            
            // If value is null/empty, provide a placeholder to allow Client construction
            if (string.IsNullOrWhiteSpace(value))
            {
                var placeholder = what switch
                {
                    "api_id" => "12345",
                    "api_hash" => "0123456789abcdef0123456789abcdef", // Valid 32-char hex string
                    "phone_number" => "+1234567890",
                    _ => null
                };
                Console.WriteLine($"📋 Config callback (placeholder): {what} = {(what == "api_hash" ? "***" : placeholder ?? "null")}");
                return placeholder;
            }
            
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
            // Check if we have saved credentials
            using var scope = _serviceProvider.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<IPlatformSettingsRepository>();
            
            var apiId = await settingsRepo.GetValueAsync("telegram:mtproto:api_id", stoppingToken);
            
            if (string.IsNullOrWhiteSpace(apiId))
            {
                Console.WriteLine("ℹ️ MTProto not configured. Set ConfigNeeded to 'ready' for web setup.");
                ConfigNeeded = "ready";
            }
            else
            {
                Console.WriteLine("ℹ️ MTProto service initialized. Authentication will be performed on first request.");
                ConfigNeeded = "ready"; // آماده برای درخواست
            }
            
            // منتظر می‌مونیم تا برنامه خاتمه پیدا کنه
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("ℹ️ MTProto service is shutting down.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MTProto background service");
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

    public async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuthenticated && Client.User != null)
        {
            return; // Already authenticated
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (_isAuthenticated && Client.User != null)
            {
                return; // Double-check after acquiring lock
            }

            Console.WriteLine("🔐 Starting lazy authentication...");
            
            using var scope = _serviceProvider.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<IPlatformSettingsRepository>();
            
            var phoneNumber = await settingsRepo.GetValueAsync("telegram:mtproto:phone_number", cancellationToken);
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                Console.WriteLine($"🔐 Logging in with phone: {phoneNumber}");
                ConfigNeeded = await DoLogin(phoneNumber);
                
                if (Client.User != null)
                {
                    _isAuthenticated = true;
                    Console.WriteLine($"✅ Authentication successful! Logged in as: {Client.User.first_name}");
                }
            }
            else
            {
                Console.WriteLine("⚠️ No phone number configured.");
                ConfigNeeded = "api_id";
            }
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task<bool> IsContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            Console.WriteLine($"🔍 Checking contact status for user {recipientTelegramUserId}...");
            
            var dialogs = await Client.Messages_GetAllDialogs();
            var user = dialogs.users.Values.OfType<User>()
                .FirstOrDefault(u => u.id == recipientTelegramUserId);

            if (user == null)
            {
                Console.WriteLine($"❌ User {recipientTelegramUserId} not found in dialogs");
                return false;
            }

            // لاگ کردن اطلاعات user
            Console.WriteLine($"📊 User Details:");
            Console.WriteLine($"  - ID: {user.id}");
            Console.WriteLine($"  - Username: {user.username}");
            Console.WriteLine($"  - First Name: {user.first_name}");
            Console.WriteLine($"  - Access Hash: {user.access_hash}");
            Console.WriteLine($"  - contact: {user.flags.HasFlag(User.Flags.contact)}");
            Console.WriteLine($"  - mutual_contact: {user.flags.HasFlag(User.Flags.mutual_contact)}");

            // مرحله 1: اگر در کانتکت نیست، از طرف فرستنده اضافه می‌کنیم
            if (!user.flags.HasFlag(User.Flags.contact))
            {
                Console.WriteLine($"⚠️ User {recipientTelegramUserId} is not in sender's contacts. Adding automatically...");
                
                try
                {
                    var inputUser = new InputUser(user.id, user.access_hash);
                    
                    // اضافه کردن با یک label خاص برای تشخیص راحت‌تر
                    var firstName = $"🤖 {user.first_name ?? "Customer"}";
                    var lastName = "[Bot Customer]";
                    
                    var result = await Client.Contacts_AddContact(
                        id: inputUser,
                        first_name: firstName,
                        last_name: lastName,
                        phone: user.phone ?? "",
                        add_phone_privacy_exception: false
                    );
                    
                    Console.WriteLine($"✅ Successfully added user {recipientTelegramUserId} to sender's contacts with label");
                    
                    // دوباره fetch می‌کنیم
                    var updatedDialogs = await Client.Messages_GetAllDialogs();
                    user = updatedDialogs.users.Values.OfType<User>()
                        .FirstOrDefault(u => u.id == recipientTelegramUserId);
                    
                    if (user == null)
                    {
                        Console.WriteLine($"❌ Failed to fetch updated user info");
                        return false;
                    }
                    
                    Console.WriteLine($"📊 Updated flags - contact: {user.flags.HasFlag(User.Flags.contact)}, mutual_contact: {user.flags.HasFlag(User.Flags.mutual_contact)}");
                }
                catch (Exception addEx)
                {
                    Console.WriteLine($"❌ Failed to add contact: {addEx.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"✅ User {recipientTelegramUserId} is already in sender's contacts");
            }
            
            // مرحله 2: چک می‌کنیم که mutual_contact هست یا نه
            // این یعنی subscriber هم باید sender رو توی کانتکتش اضافه کرده باشه
            bool isMutualContact = user.flags.HasFlag(User.Flags.mutual_contact);
            
            if (!isMutualContact)
            {
                Console.WriteLine($"⚠️ Not mutual contact! User {recipientTelegramUserId} has NOT added sender to their contacts.");
                Console.WriteLine($"❌ Cannot send self-destructing media without mutual contact.");
                return false;
            }
            
            Console.WriteLine($"✅ Mutual contact confirmed! Both parties have each other in contacts.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking contact: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<ContentDeliveryResult> SendPhotoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        Domain.Entities.Photo photoEntity,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        string? tempFilePath = null;
        
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
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

            // چک می‌کنیم که آیا cached MTProto info داریم
            if (photoEntity != null && photoEntity.HasMtProtoPhotoInfo())
            {
                Console.WriteLine($"✅ Using cached MTProto photo info (ID: {photoEntity.MtProtoPhotoId})");
                
                byte[] currentFileReference = photoEntity.MtProtoFileReference!;
                
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        // استفاده از cached photo برای ارسال
                        var inputPhoto = new InputPhoto
                        {
                            id = photoEntity.MtProtoPhotoId!.Value,
                            access_hash = photoEntity.MtProtoAccessHash!.Value,
                            file_reference = currentFileReference
                        };
                        
                        var cachedMedia = new InputMediaPhoto
                        {
                            id = inputPhoto,
                            flags = InputMediaPhoto.Flags.has_ttl_seconds,
                            ttl_seconds = selfDestructSeconds
                        };

                        Console.WriteLine($"📤 Sending cached photo with {selfDestructSeconds}s timer... (attempt {attempt + 1})");
                        var sendResult = await Client.Messages_SendMedia(user, cachedMedia, caption ?? "", DateTime.UtcNow.Ticks);
                        
                        // ذخیره message ID برای refresh های بعدی
                        if (sendResult is Updates updatesResult)
                        {
                            var sentMsg = updatesResult.updates.OfType<UpdateNewMessage>()
                                .Select(x => x.message)
                                .OfType<Message>()
                                .FirstOrDefault();
                                
                            if (sentMsg != null && sentMsg.media is MessageMediaPhoto mmp && mmp.photo is TL.Photo photo)
                            {
                                // Update file_reference و message ID
                                photoEntity.SetMtProtoPhotoInfo(photo.ID, photo.access_hash, photo.file_reference, sentMsg.ID);
                                Console.WriteLine($"💾 Updated file_reference and saved message ID: {sentMsg.ID}");
                            }
                        }
                        
                        Console.WriteLine($"✅ Photo sent successfully using cache!");
                        return ContentDeliveryResult.Success();
                    }
                    catch (RpcException rpcEx) when (attempt == 0 && rpcEx.Code == 400 && rpcEx.Message.Contains("FILE_REFERENCE_"))
                    {
                        Console.WriteLine($"⚠️ File reference expired: {rpcEx.Message}");
                        
                        // سعی می‌کنیم file_reference جدید بگیریم
                        if (photoEntity.MtProtoLastMessageId.HasValue)
                        {
                            try
                            {
                                Console.WriteLine($"🔄 Refreshing file_reference from message ID: {photoEntity.MtProtoLastMessageId}");
                                
                                var messages = await Client.Messages_GetMessages(new[] { new InputMessageID { id = photoEntity.MtProtoLastMessageId.Value } });
                                
                                if (messages.Messages.Length > 0 && messages.Messages[0] is Message msg)
                                {
                                    if (msg.media is MessageMediaPhoto mmp && mmp.photo is TL.Photo photo)
                                    {
                                        currentFileReference = photo.file_reference;
                                        photoEntity.UpdateMtProtoFileReference(currentFileReference);
                                        Console.WriteLine($"✅ File reference refreshed successfully!");
                                        continue; // تلاش مجدد با file_reference جدید
                                    }
                                }
                                
                                Console.WriteLine($"⚠️ Could not extract photo from message, falling back to upload...");
                            }
                            catch (Exception refreshEx)
                            {
                                Console.WriteLine($"⚠️ Failed to refresh file_reference: {refreshEx.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ No message ID available for refresh, falling back to upload...");
                        }
                        
                        break; // خارج شدن از loop و رفتن به upload
                    }
                    catch (Exception cacheEx)
                    {
                        Console.WriteLine($"⚠️ Failed to send cached photo: {cacheEx.Message}");
                        break; // خارج شدن از loop و رفتن به upload
                    }
                }
            }

            // اگر cache نداشتیم یا ارسال cache fail شد، باید upload کنیم
            string fileToUpload = filePath;
            
            // تشخیص اینکه filePath یک Telegram file ID است یا فایل محلی
            if (!File.Exists(filePath) && !filePath.Contains("/") && !filePath.Contains("\\"))
            {
                // این یک Telegram file ID است - باید دانلود کنیم
                Console.WriteLine($"📥 Detected Telegram file ID: {filePath}. Downloading...");
                
                tempFilePath = Path.Combine(Path.GetTempPath(), $"telegram_photo_{Guid.NewGuid()}.jpg");
                
                try
                {
                    // دانلود فایل از Bot API
                    var file = await _botClient.GetFileAsync(filePath, cancellationToken);
                    
                    if (file.FilePath == null)
                    {
                        Console.WriteLine($"❌ Failed to get file path from Telegram");
                        return ContentDeliveryResult.Failure("Failed to download photo from Telegram");
                    }
                    
                    // دانلود به فایل موقت
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        await _botClient.DownloadFileAsync(file.FilePath, fileStream, cancellationToken);
                    }
                    
                    Console.WriteLine($"✅ Downloaded to temp file: {tempFilePath}");
                    fileToUpload = tempFilePath;
                }
                catch (Exception downloadEx)
                {
                    Console.WriteLine($"❌ Error downloading file: {downloadEx.Message}");
                    return ContentDeliveryResult.Failure($"Failed to download photo: {downloadEx.Message}");
                }
            }
            else if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ File not found: {filePath}");
                return ContentDeliveryResult.Failure($"File not found: {filePath}");
            }

            // Upload file به MTProto
            Console.WriteLine($"📤 Uploading file to MTProto: {fileToUpload}");
            var inputFile = await Client.UploadFileAsync(fileToUpload, null);
            
            // Create media with TTL
            var media = new InputMediaUploadedPhoto
            {
                file = inputFile,
                flags = InputMediaUploadedPhoto.Flags.has_ttl_seconds,
                ttl_seconds = selfDestructSeconds
            };

            // Send media
            Console.WriteLine($"📤 Sending uploaded photo with {selfDestructSeconds}s timer...");
            var result = await Client.Messages_SendMedia(user, media, caption ?? "", DateTime.UtcNow.Ticks);

            // Extract photo info from result for caching
            if (photoEntity != null && result is Updates updates)
            {
                var sentMsg = updates.updates.OfType<UpdateNewMessage>()
                    .Select(x => x.message)
                    .OfType<Message>()
                    .FirstOrDefault();
                    
                if (sentMsg?.media is MessageMediaPhoto mmp && mmp.photo is TL.Photo photo)
                {
                    Console.WriteLine($"💾 Caching MTProto photo info (ID: {photo.ID}, MessageID: {sentMsg.ID})");
                    photoEntity.SetMtProtoPhotoInfo(photo.ID, photo.access_hash, photo.file_reference, sentMsg.ID);
                }
            }

            Console.WriteLine($"✅ Photo sent successfully!");
            return ContentDeliveryResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending photo: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return ContentDeliveryResult.Failure($"Error: {ex.Message}");
        }
        finally
        {
            // پاک کردن فایل موقت
            if (tempFilePath != null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    Console.WriteLine($"🗑️ Deleted temp file: {tempFilePath}");
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"⚠️ Failed to delete temp file: {cleanupEx.Message}");
                }
            }
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

