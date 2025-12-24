using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TL;
using WTelegram;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Implementation of MTProto service for Telegram User API using WTelegramClient
/// </summary>
public class MtProtoService : IMtProtoService, IAsyncDisposable
{
    private readonly Client _client;
    private readonly string _phoneNumber;
    private bool _isAuthenticated;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public MtProtoService(string apiId, string apiHash, string phoneNumber, string? sessionPath = null)
    {
        _phoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        
        // Initialize WTelegram Client
        _client = new Client(Config);
        
        string? Config(string what)
        {
            return what switch
            {
                "api_id" => apiId,
                "api_hash" => apiHash,
                "phone_number" => _phoneNumber,
                "session_pathname" => sessionPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mtproto_session.dat"),
                "verification_code" => WaitForVerificationCode(),
                "password" => WaitFor2FAPassword(),
                _ => null
            };
        }
    }

    private static string? WaitForVerificationCode()
    {
        Console.WriteLine("⏳ Waiting for verification code...");
        Console.WriteLine("💬 Admin: Send the verification code to the bot using: /auth_code <your_code>");
        
        // Wait up to 5 minutes for the code
        var timeout = DateTime.UtcNow.AddMinutes(5);
        while (DateTime.UtcNow < timeout)
        {
            var code = MtProtoAuthStore.GetAndRemoveVerificationCode();
            if (!string.IsNullOrEmpty(code))
            {
                return code;
            }
            Thread.Sleep(500); // Check every 500ms
        }
        
        Console.WriteLine("❌ Timeout waiting for verification code");
        return null;
    }
    
    private static string? WaitFor2FAPassword()
    {
        Console.WriteLine("⏳ Waiting for 2FA password...");
        Console.WriteLine("💬 Admin: Send your 2FA password to the bot using: /auth_password <your_password>");
        
        // Wait up to 5 minutes for the password
        var timeout = DateTime.UtcNow.AddMinutes(5);
        while (DateTime.UtcNow < timeout)
        {
            var password = MtProtoAuthStore.GetAndRemove2FAPassword();
            if (!string.IsNullOrEmpty(password))
            {
                return password;
            }
            Thread.Sleep(500); // Check every 500ms
        }
        
        Console.WriteLine("❌ Timeout waiting for 2FA password");
        return null;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuthenticated) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isAuthenticated) return;

            // Login to Telegram
            var myself = await _client.LoginUserIfNeeded();
            
            if (myself == null)
            {
                throw new InvalidOperationException("Failed to authenticate with Telegram. Check your credentials.");
            }

            _isAuthenticated = true;
            Console.WriteLine($"✅ MTProto authenticated as: {myself.username ?? myself.first_name} (ID: {myself.id})");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<bool> IsContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // We need to check if WE are in THEIR contacts, not if they're in ours
            // Try to resolve the user and check their contact status
            try
            {
                var resolvedPeer = await _client.Contacts_ResolveUsername(recipientTelegramUserId.ToString());
                if (resolvedPeer?.users != null && resolvedPeer.users.TryGetValue(recipientTelegramUserId, out var user))
                {
                    if (user is User u)
                    {
                        // Check if they have us as a contact (mutual_contact or contact)
                        Console.WriteLine($"🔍 Contact check for user {recipientTelegramUserId}: mutual_contact={u.flags.HasFlag(User.Flags.mutual_contact)}, contact={u.flags.HasFlag(User.Flags.contact)}");
                        return u.flags.HasFlag(User.Flags.mutual_contact) || u.flags.HasFlag(User.Flags.contact);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not resolve user by username, trying alternative method: {ex.Message}");
            }

            // Alternative: Check if user is in our contacts (mutual contact scenario)
            var contacts = await _client.Contacts_GetContacts();
            
            if (contacts.users != null && contacts.users.TryGetValue(recipientTelegramUserId, out var contactUser))
            {
                if (contactUser is User u)
                {
                    // If they're in our contacts and marked as mutual_contact, they have us too
                    Console.WriteLine($"🔍 Found in contacts: mutual_contact={u.flags.HasFlag(User.Flags.mutual_contact)}");
                    return u.flags.HasFlag(User.Flags.mutual_contact);
                }
            }

            Console.WriteLine($"❌ User {recipientTelegramUserId} has not added sender account to their contacts");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking contact status: {ex.Message}");
            return false;
        }
    }

    public async Task<ContentDeliveryResult> SendPhotoWithTimerAsync(
        long recipientTelegramUserId,
        string filePathOrFileId,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Validate contact first
            var isContact = await IsContactAsync(recipientTelegramUserId, cancellationToken);
            if (!isContact)
            {
                return ContentDeliveryResult.Failure("❌ Recipient must have sender account in their contacts first");
            }

            // Get the recipient peer
            var inputPeer = new InputPeerUser(recipientTelegramUserId, 0);

            InputFile inputFile;
            
            // Check if it's a file ID or file path
            if (!filePathOrFileId.Contains(Path.DirectorySeparatorChar) && 
                !filePathOrFileId.Contains(Path.AltDirectorySeparatorChar) &&
                !File.Exists(filePathOrFileId))
            {
                // It's likely a Telegram file ID - we can't use it directly with MTProto
                // We need to download it first or use the Bot API photo
                return ContentDeliveryResult.Failure("⚠️ Cannot send Telegram file IDs via MTProto. Please use local file path.");
            }
            else
            {
                // It's a local file path
                if (!File.Exists(filePathOrFileId))
                {
                    return ContentDeliveryResult.Failure($"❌ Photo file not found: {filePathOrFileId}");
                }

                // Upload the photo file
                Console.WriteLine($"📤 Uploading photo to Telegram servers...");
                var uploadedFile = await _client.UploadFileAsync(filePathOrFileId);
                inputFile = (InputFile)uploadedFile;
            }

            // Send photo with self-destruct timer
            var inputMediaPhoto = new InputMediaUploadedPhoto
            {
                file = inputFile,
                ttl_seconds = selfDestructSeconds
            };

            Console.WriteLine($"📨 Sending photo with {selfDestructSeconds}s timer to user {recipientTelegramUserId}...");
            
            var sentMessage = await _client.Messages_SendMedia(
                peer: inputPeer,
                media: inputMediaPhoto,
                message: caption ?? "",
                random_id: Random.Shared.NextInt64());

            if (sentMessage != null)
            {
                Console.WriteLine($"✅ Photo sent successfully with self-destruct timer!");
                return ContentDeliveryResult.Success();
            }
            else
            {
                return ContentDeliveryResult.Failure("❌ Failed to send photo");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending photo: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return ContentDeliveryResult.Failure($"Failed to send photo: {ex.Message}");
        }
    }

    public async Task<ContentDeliveryResult> SendVideoWithTimerAsync(
        long recipientTelegramUserId,
        string filePathOrFileId,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Validate contact first
            var isContact = await IsContactAsync(recipientTelegramUserId, cancellationToken);
            if (!isContact)
            {
                return ContentDeliveryResult.Failure("❌ Recipient must have sender account in their contacts first");
            }

            // Get the recipient peer
            var inputPeer = new InputPeerUser(recipientTelegramUserId, 0);

            InputFile inputFile;
            
            // Check if it's a file ID or file path
            if (!filePathOrFileId.Contains(Path.DirectorySeparatorChar) && 
                !filePathOrFileId.Contains(Path.AltDirectorySeparatorChar) &&
                !File.Exists(filePathOrFileId))
            {
                // It's likely a Telegram file ID
                return ContentDeliveryResult.Failure("⚠️ Cannot send Telegram file IDs via MTProto. Please use local file path.");
            }
            else
            {
                // It's a local file path
                if (!File.Exists(filePathOrFileId))
                {
                    return ContentDeliveryResult.Failure($"❌ Video file not found: {filePathOrFileId}");
                }

                // Upload the video file
                Console.WriteLine($"📤 Uploading video to Telegram servers...");
                var uploadedFile = await _client.UploadFileAsync(filePathOrFileId);
                inputFile = (InputFile)uploadedFile;
            }

            // Send video with self-destruct timer
            var inputMediaVideo = new InputMediaUploadedDocument
            {
                file = inputFile,
                mime_type = "video/mp4",
                ttl_seconds = selfDestructSeconds,
                attributes = new[]
                {
                    new DocumentAttributeVideo
                    {
                        duration = 0, // Can be set if duration is known
                        w = 0,        // Width
                        h = 0         // Height
                    }
                }
            };

            Console.WriteLine($"📨 Sending video with {selfDestructSeconds}s timer to user {recipientTelegramUserId}...");
            
            var sentMessage = await _client.Messages_SendMedia(
                peer: inputPeer,
                media: inputMediaVideo,
                message: caption ?? "",
                random_id: Random.Shared.NextInt64());

            if (sentMessage != null)
            {
                Console.WriteLine($"✅ Video sent successfully with self-destruct timer!");
                return ContentDeliveryResult.Success();
            }
            else
            {
                return ContentDeliveryResult.Failure("❌ Failed to send video");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending video: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return ContentDeliveryResult.Failure($"Failed to send video: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        _initLock?.Dispose();
    }
}
