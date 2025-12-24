# Telegram API Guide: Sending Photos Securely

## Overview

This guide explains how to send photos securely using Telegram APIs. Our system uses **two different approaches**:

1. **Telegram Bot API** - For regular bot messages and communication
2. **Telegram User API (MTProto)** - For timed/self-destructing media delivery

---

## Part 1: Telegram Bot API (Regular Messages)

### Prerequisites

Install the `Telegram.Bot` NuGet package:
```bash
dotnet add package Telegram.Bot
```

### Basic Photo Sending

```csharp
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

public class TelegramBotService
{
    private readonly TelegramBotClient _botClient;

    public TelegramBotService(string botToken)
    {
        _botClient = new TelegramBotClient(botToken);
    }

    // Method 1: Send photo from file path
    public async Task<Message> SendPhotoFromFileAsync(
        long chatId, 
        string filePath, 
        string? caption = null)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var inputFile = new InputOnlineFile(fileStream, Path.GetFileName(filePath));
        
        return await _botClient.SendPhotoAsync(
            chatId: chatId,
            photo: inputFile,
            caption: caption);
    }

    // Method 2: Send photo from URL
    public async Task<Message> SendPhotoFromUrlAsync(
        long chatId, 
        string photoUrl, 
        string? caption = null)
    {
        return await _botClient.SendPhotoAsync(
            chatId: chatId,
            photo: photoUrl,
            caption: caption);
    }

    // Method 3: Send photo from file ID (already uploaded)
    public async Task<Message> SendPhotoFromFileIdAsync(
        long chatId, 
        string fileId, 
        string? caption = null)
    {
        return await _botClient.SendPhotoAsync(
            chatId: chatId,
            photo: fileId,
            caption: caption);
    }
}
```

### Security Considerations for Bot API

#### 1. **File Validation**
```csharp
private bool IsValidImageFile(string filePath)
{
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    
    if (!allowedExtensions.Contains(extension))
        return false;

    // Check MIME type
    var mimeType = GetMimeType(filePath);
    if (!mimeType.StartsWith("image/"))
        return false;

    // Check file size (max 10MB for photos)
    var fileInfo = new FileInfo(filePath);
    if (fileInfo.Length > 10 * 1024 * 1024)
        return false;

    return true;
}

private string GetMimeType(string filePath)
{
    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
```

#### 2. **Error Handling**
```csharp
public async Task<bool> SendPhotoSafelyAsync(
    long chatId, 
    string filePath, 
    string? caption = null)
{
    try
    {
        // Validate file
        if (!IsValidImageFile(filePath))
        {
            throw new ArgumentException("Invalid image file");
        }

        // Check if file exists
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Photo file not found");
        }

        // Send photo
        await SendPhotoFromFileAsync(chatId, filePath, caption);
        return true;
    }
    catch (ApiRequestException ex)
    {
        // Handle Telegram API errors
        Console.WriteLine($"Telegram API Error: {ex.Message}");
        return false;
    }
    catch (Exception ex)
    {
        // Handle other errors
        Console.WriteLine($"Error sending photo: {ex.Message}");
        return false;
    }
}
```

#### 3. **Rate Limiting**
```csharp
private readonly SemaphoreSlim _rateLimiter = new(1, 1);
private DateTime _lastRequestTime = DateTime.MinValue;
private const int MinDelayMs = 100; // Minimum 100ms between requests

public async Task<Message> SendPhotoWithRateLimitAsync(
    long chatId, 
    string filePath, 
    string? caption = null)
{
    await _rateLimiter.WaitAsync();
    try
    {
        var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
        if (timeSinceLastRequest.TotalMilliseconds < MinDelayMs)
        {
            await Task.Delay(MinDelayMs - (int)timeSinceLastRequest.TotalMilliseconds);
        }

        var message = await SendPhotoFromFileAsync(chatId, filePath, caption);
        _lastRequestTime = DateTime.UtcNow;
        return message;
    }
    finally
    {
        _rateLimiter.Release();
    }
}
```

---

## Part 2: Telegram User API (MTProto) - For Timed Media

### Why MTProto for Our Use Case?

- **Bot API Limitation**: Bot API does NOT support self-destructing/timed media
- **User API Feature**: Supports `Messages_SetHistoryTTL` for timed messages
- **Required Feature**: Our system needs timed/self-destructing media

### Prerequisites

Install `WTelegramClient` NuGet package:
```bash
dotnet add package WTelegramClient
```

### Complete MTProto Implementation

```csharp
using WTelegramClient;
using TL;

namespace TelegramPhotoBot.Infrastructure.Services;

public class MtProtoService : IMtProtoService
{
    private readonly Client _client;
    private readonly string _apiId;
    private readonly string _apiHash;
    private readonly string _phoneNumber;
    private bool _isConnected = false;

    public MtProtoService(string apiId, string apiHash, string phoneNumber)
    {
        _apiId = apiId;
        _apiHash = apiHash;
        _phoneNumber = phoneNumber;
        
        // Initialize client
        _client = new Client(int.Parse(apiId), apiHash);
    }

    /// <summary>
    /// Connect to Telegram and authenticate
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected) return;

        // Handle authentication
        _client.OnUpdate += (update) => { /* Handle updates */ };
        
        var loginInfo = await _client.LoginUserIfNeeded();
        if (loginInfo != null)
        {
            // Two-factor authentication or phone code required
            throw new InvalidOperationException($"Authentication required: {loginInfo}");
        }

        _isConnected = true;
    }

    /// <summary>
    /// Check if recipient has sender in contacts
    /// </summary>
    public async Task<bool> IsContactAsync(
        long recipientTelegramUserId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            // Get contacts
            var contacts = await _client.Contacts_GetContacts();
            
            // Check if recipient is in contacts
            var isContact = contacts.users.Values
                .Any(u => u.id == recipientTelegramUserId);

            return isContact;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking contact: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send photo with self-destruct timer via MTProto
    /// </summary>
    public async Task<ContentDeliveryResult> SendPhotoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            // 1. Validate contact first (SECURITY REQUIREMENT)
            var isContact = await IsContactAsync(recipientTelegramUserId, cancellationToken);
            if (!isContact)
            {
                return ContentDeliveryResult.Failure(
                    "Please add this account to your contacts first");
            }

            // 2. Validate file
            if (!File.Exists(filePath))
            {
                return ContentDeliveryResult.Failure("Photo file not found");
            }

            if (!IsValidImageFile(filePath))
            {
                return ContentDeliveryResult.Failure("Invalid image file");
            }

            // 3. Get recipient user
            var users = await _client.Contacts_ResolveUsername(recipientTelegramUserId.ToString());
            var recipient = users.users.Values.FirstOrDefault(u => u.id == recipientTelegramUserId);
            if (recipient == null)
            {
                return ContentDeliveryResult.Failure("Recipient not found");
            }

            var inputPeer = new InputPeerUser 
            { 
                user_id = recipientTelegramUserId,
                access_hash = recipient.access_hash 
            };

            // 4. Upload photo file
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var uploadedFile = await _client.Upload_File(
                new InputFile 
                { 
                    name = Path.GetFileName(filePath),
                    data = fileBytes 
                });

            // 5. Create photo media
            var photoMedia = new InputMediaUploadedPhoto
            {
                file = uploadedFile,
                caption = caption ?? string.Empty
            };

            // 6. Send photo
            var sentMessage = await _client.Messages_SendMedia(
                peer: inputPeer,
                media: photoMedia,
                message: caption ?? string.Empty,
                random_id: Environment.TickCount64);

            // 7. Set self-destruct timer (CRITICAL FEATURE)
            await _client.Messages_SetHistoryTTL(
                peer: inputPeer,
                period: selfDestructSeconds);

            return ContentDeliveryResult.Success(sentMessage.id.ToString());
        }
        catch (Exception ex)
        {
            return ContentDeliveryResult.Failure($"Failed to send photo: {ex.Message}");
        }
    }

    /// <summary>
    /// Send video with self-destruct timer
    /// </summary>
    public async Task<ContentDeliveryResult> SendVideoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            // 1. Validate contact
            var isContact = await IsContactAsync(recipientTelegramUserId, cancellationToken);
            if (!isContact)
            {
                return ContentDeliveryResult.Failure(
                    "Please add this account to your contacts first");
            }

            // 2. Validate file
            if (!File.Exists(filePath))
            {
                return ContentDeliveryResult.Failure("Video file not found");
            }

            // 3. Get recipient
            var users = await _client.Contacts_ResolveUsername(recipientTelegramUserId.ToString());
            var recipient = users.users.Values.FirstOrDefault(u => u.id == recipientTelegramUserId);
            if (recipient == null)
            {
                return ContentDeliveryResult.Failure("Recipient not found");
            }

            var inputPeer = new InputPeerUser 
            { 
                user_id = recipientTelegramUserId,
                access_hash = recipient.access_hash 
            };

            // 4. Upload video file
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var uploadedFile = await _client.Upload_File(
                new InputFile 
                { 
                    name = Path.GetFileName(filePath),
                    data = fileBytes 
                });

            // 5. Create video media
            var videoMedia = new InputMediaUploadedDocument
            {
                file = uploadedFile,
                mime_type = "video/mp4",
                attributes = new[] { new DocumentAttributeVideo() },
                caption = caption ?? string.Empty
            };

            // 6. Send video
            var sentMessage = await _client.Messages_SendMedia(
                peer: inputPeer,
                media: videoMedia,
                message: caption ?? string.Empty,
                random_id: Environment.TickCount64);

            // 7. Set self-destruct timer
            await _client.Messages_SetHistoryTTL(
                peer: inputPeer,
                period: selfDestructSeconds);

            return ContentDeliveryResult.Success(sentMessage.id.ToString());
        }
        catch (Exception ex)
        {
            return ContentDeliveryResult.Failure($"Failed to send video: {ex.Message}");
        }
    }

    private bool IsValidImageFile(string filePath)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
```

---

## Security Best Practices

### 1. **File Validation**

```csharp
public class SecureFileValidator
{
    private static readonly string[] AllowedMimeTypes = 
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private static readonly long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public static ValidationResult ValidatePhotoFile(string filePath)
    {
        // Check file exists
        if (!File.Exists(filePath))
            return ValidationResult.Failure("File does not exist");

        var fileInfo = new FileInfo(filePath);

        // Check file size
        if (fileInfo.Length > MaxFileSize)
            return ValidationResult.Failure("File size exceeds maximum allowed (10MB)");

        // Check extension
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!IsAllowedExtension(extension))
            return ValidationResult.Failure("File type not allowed");

        // Verify MIME type matches extension
        var mimeType = GetMimeType(filePath);
        if (!AllowedMimeTypes.Contains(mimeType))
            return ValidationResult.Failure("Invalid MIME type");

        // Optional: Verify file is actually an image (read header)
        if (!IsValidImageHeader(filePath))
            return ValidationResult.Failure("File is not a valid image");

        return ValidationResult.Success();
    }

    private static bool IsValidImageHeader(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var header = new byte[8];
            stream.Read(header, 0, 8);

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                return true;

            // GIF: 47 49 46 38
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}
```

### 2. **Secure File Storage**

```csharp
public class SecureFileStorage
{
    private readonly string _basePath;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };

    public SecureFileStorage(string basePath)
    {
        _basePath = Path.GetFullPath(basePath);
        
        // Ensure directory exists
        Directory.CreateDirectory(_basePath);
    }

    public string SavePhoto(Stream fileStream, string originalFileName)
    {
        // Sanitize filename
        var sanitizedFileName = SanitizeFileName(originalFileName);
        
        // Generate unique filename to prevent conflicts
        var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
        var filePath = Path.Combine(_basePath, uniqueFileName);

        // Validate extension
        var extension = Path.GetExtension(uniqueFileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            throw new ArgumentException("Invalid file extension");

        // Save file
        using var outputStream = new FileStream(filePath, FileMode.Create);
        fileStream.CopyTo(outputStream);

        return filePath;
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove path traversal attempts
        var sanitized = Path.GetFileName(fileName);
        
        // Remove dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }
}
```

### 3. **Access Control**

```csharp
public class ContentAccessController
{
    private readonly IContentAuthorizationService _authorizationService;
    private readonly IContentDeliveryService _deliveryService;

    public async Task<DeliveryResult> DeliverPhotoSecurelyAsync(
        Guid userId,
        Guid photoId,
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        // 1. Check authorization
        var accessResult = await _authorizationService
            .CheckPhotoAccessAsync(userId, photoId, cancellationToken);
        
        if (!accessResult.HasAccess)
        {
            return DeliveryResult.Denied(accessResult.Reason);
        }

        // 2. Validate contact (SECURITY REQUIREMENT)
        var isContact = await _deliveryService
            .ValidateContactAsync(telegramUserId, cancellationToken);
        
        if (!isContact)
        {
            return DeliveryResult.Denied(
                "Please add this account to your contacts first");
        }

        // 3. Get photo file path
        var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
        if (photo == null)
        {
            return DeliveryResult.Denied("Photo not found");
        }

        // 4. Deliver via MTProto with timer
        var request = new SendPhotoRequest
        {
            RecipientTelegramUserId = telegramUserId,
            FilePath = photo.FileInfo.FilePath,
            Caption = photo.Caption,
            SelfDestructSeconds = 60 // 1 minute
        };

        return await _deliveryService.SendPhotoAsync(request, cancellationToken);
    }
}
```

### 4. **Error Handling & Logging**

```csharp
public class SecurePhotoSender
{
    private readonly ILogger<SecurePhotoSender> _logger;

    public async Task<bool> SendPhotoWithLoggingAsync(
        long chatId,
        string filePath,
        string? caption = null)
    {
        try
        {
            // Log attempt
            _logger.LogInformation(
                "Attempting to send photo to chat {ChatId}, file: {FilePath}",
                chatId, filePath);

            // Validate
            if (!SecureFileValidator.ValidatePhotoFile(filePath).IsValid)
            {
                _logger.LogWarning("Invalid photo file: {FilePath}", filePath);
                return false;
            }

            // Send
            await SendPhotoFromFileAsync(chatId, filePath, caption);
            
            _logger.LogInformation(
                "Successfully sent photo to chat {ChatId}", chatId);
            
            return true;
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 403)
        {
            _logger.LogWarning(
                "Access denied sending photo to chat {ChatId}: {Error}",
                chatId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending photo to chat {ChatId}, file: {FilePath}",
                chatId, filePath);
            return false;
        }
    }
}
```

---

## Configuration

### appsettings.json

```json
{
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN",
    "MtProto": {
      "ApiId": "YOUR_API_ID",
      "ApiHash": "YOUR_API_HASH",
      "PhoneNumber": "+1234567890"
    }
  },
  "FileStorage": {
    "BasePath": "/secure/photos",
    "MaxFileSize": 10485760,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"]
  }
}
```

---

## Summary

### Bot API (Regular Messages)
- ✅ Simple to use
- ✅ Good for regular bot communication
- ❌ **Cannot send timed/self-destructing media**

### MTProto/User API (Timed Media)
- ✅ Supports self-destructing media
- ✅ Required for our use case
- ⚠️ More complex setup
- ⚠️ Requires user account (not bot)

### Security Checklist
- ✅ Validate file type and size
- ✅ Check file headers (not just extension)
- ✅ Sanitize filenames
- ✅ Validate recipient is in contacts (MTProto)
- ✅ Rate limiting
- ✅ Error handling and logging
- ✅ Access control before delivery

---

## Next Steps

1. Install required packages:
   ```bash
   dotnet add package Telegram.Bot
   dotnet add package WTelegramClient
   ```

2. Update `MtProtoService.cs` with the complete implementation above

3. Update `TelegramBotService.cs` with Bot API implementation

4. Configure credentials in `appsettings.json`

5. Test with small files first

6. Implement logging and monitoring

