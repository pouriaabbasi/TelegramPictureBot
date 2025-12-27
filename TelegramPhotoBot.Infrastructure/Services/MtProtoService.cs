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
    private static readonly object _sessionFileLock = new object();
    private static readonly HashSet<string> _activeSessionFiles = new HashSet<string>();
    
    private Client _client;
    private string _apiId;
    private string _apiHash;
    private string _phoneNumber;
    private string? _sessionPath;
    private bool _isAuthenticated;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _reinitLock = new(1, 1);
    private DateTime? _lastAuthAttempt;
    private const int AuthRetryDelaySeconds = 60; // Wait 60 seconds before retrying after failure

    public MtProtoService(string apiId, string apiHash, string phoneNumber, string? sessionPath = null)
    {
        _apiId = apiId ?? throw new ArgumentNullException(nameof(apiId));
        _apiHash = apiHash ?? throw new ArgumentNullException(nameof(apiHash));
        _phoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        _sessionPath = sessionPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
        
        // Ensure session file directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_sessionPath)!);
        
        // Check if session file is already in use
        lock (_sessionFileLock)
        {
            if (_activeSessionFiles.Contains(_sessionPath))
            {
                // If same session file is in use, use a unique one
                var basePath = Path.Combine(Path.GetDirectoryName(_sessionPath)!, Path.GetFileNameWithoutExtension(_sessionPath));
                var extension = Path.GetExtension(_sessionPath);
                var uniquePath = $"{basePath}_{Guid.NewGuid():N}{extension}";
                Console.WriteLine($"⚠️ Session file {_sessionPath} is already in use. Using unique path: {uniquePath}");
                _sessionPath = uniquePath;
            }
            _activeSessionFiles.Add(_sessionPath);
        }
        
        // Helper method to create client with retry logic for corrupt session files
        Client CreateClientWithRetry()
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // If this is a placeholder service, delete any existing session file first
                    if (_apiId == "0" || _apiHash == "placeholder" || _phoneNumber == "+0000000000")
                    {
                        if (File.Exists(_sessionPath))
                        {
                            try
                            {
                                File.Delete(_sessionPath);
                                Console.WriteLine($"🧹 Deleted existing session file for placeholder service: {_sessionPath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Could not delete session file: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        // Check if session file exists and might be corrupt
                        if (File.Exists(_sessionPath))
                        {
                            try
                            {
                                // Try to read the file to check if it's valid hex
                                var fileContent = File.ReadAllText(_sessionPath);
                                // If file is empty or invalid hex length, delete it
                                if (string.IsNullOrWhiteSpace(fileContent) || fileContent.Trim().Length % 2 != 0)
                                {
                                    Console.WriteLine($"⚠️ Corrupt session file detected (invalid length). Deleting: {_sessionPath}");
                                    File.Delete(_sessionPath);
                                }
                                else
                                {
                                    // Try to validate hex format
                                    try
                                    {
                                        Convert.FromHexString(fileContent.Trim());
                                    }
                                    catch (FormatException)
                                    {
                                        // File is corrupt (invalid hex), delete it
                                        Console.WriteLine($"⚠️ Corrupt session file detected (invalid hex format). Deleting: {_sessionPath}");
                                        File.Delete(_sessionPath);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Error checking session file: {ex.Message}. Attempting to delete and recreate.");
                                try
                                {
                                    File.Delete(_sessionPath);
                                }
                                catch
                                {
                                    // Ignore delete errors
                                }
                            }
                        }
                    }
                    
                    // Initialize WTelegram Client
                    return new Client(Config);
                }
                catch (FormatException ex) when ((ex.Message.Contains("hex string") || ex.Message.Contains("not a valid hex")) && attempt < maxRetries)
                {
                    // Session file is corrupt, try to delete it and retry
                    Console.WriteLine($"⚠️ Session file is corrupt during initialization (attempt {attempt}/{maxRetries}). Attempting to delete and retry: {_sessionPath}");
                    try
                    {
                        if (File.Exists(_sessionPath))
                        {
                            File.Delete(_sessionPath);
                            // Wait a bit before retry
                            Thread.Sleep(100);
                        }
                    }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"⚠️ Could not delete corrupt session file: {deleteEx.Message}");
                    }
                    
                    // Continue to next retry
                    continue;
                }
                catch (FormatException ex) when (ex.Message.Contains("hex string") || ex.Message.Contains("not a valid hex"))
                {
                    // Last attempt failed, throw the exception
                    Console.WriteLine($"❌ Failed to initialize after {maxRetries} attempts. Session file is corrupt: {_sessionPath}");
                    throw;
                }
            }
            
            // Should never reach here, but just in case
            throw new InvalidOperationException("Failed to create WTelegramClient after multiple retries");
        }
        
        try
        {
            _client = CreateClientWithRetry();
        }
        catch
        {
            // Remove from active files if initialization fails
            lock (_sessionFileLock)
            {
                _activeSessionFiles.Remove(_sessionPath);
            }
            throw;
        }
    }

    private string? Config(string what)
    {
        return what switch
        {
            "api_id" => _apiId,
            "api_hash" => _apiHash,
            "phone_number" => _phoneNumber,
            "session_pathname" => _sessionPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mtproto_session.dat"),
            // Don't block here - return null to let WTelegram handle it
            _ => null
        };
    }

    private static string? WaitForVerificationCode()
    {
        Console.WriteLine("⏳ Waiting for verification code...");
        Console.WriteLine("💬 Admin: Send the verification code to the bot using: /auth_code <your_code>");
        Console.WriteLine("📱 Note: The code is sent to your Telegram app (not SMS). Check your Telegram app notifications or the app itself.");
        
        // Notify that verification code is needed
        MtProtoAuthStore.NotifyVerificationCodeNeeded();
        
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
        
        // Notify that 2FA password is needed
        MtProtoAuthStore.Notify2FAPasswordNeeded();
        
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

    /// <summary>
    /// Performs login with the provided value (verification code or password)
    /// This is the proper way to handle authentication without blocking the Config callback
    /// </summary>
    public async Task<string?> LoginAsync(string loginInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔐 Performing login with provided info...");
            var result = await _client.Login(loginInfo);
            
            if (result == null)
            {
                // Login successful
                _isAuthenticated = true;
                _lastAuthAttempt = null;
                Console.WriteLine($"✅ MTProto authenticated successfully!");
                MtProtoAuthStore.NotifyAuthenticationSuccess();
                return null;
            }
            else
            {
                // More info needed (verification_code, password, etc.)
                Console.WriteLine($"ℹ️ Login needs: {result}");
                
                if (result == "verification_code")
                {
                    MtProtoAuthStore.NotifyVerificationCodeNeeded();
                }
                else if (result == "password")
                {
                    MtProtoAuthStore.Notify2FAPasswordNeeded();
                }
                
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Login error: {ex.Message}");
            _isAuthenticated = false;
            throw;
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuthenticated) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isAuthenticated) return;

            // Check if we should retry (wait at least AuthRetryDelaySeconds after last failed attempt)
            if (_lastAuthAttempt.HasValue)
            {
                var timeSinceLastAttempt = DateTime.UtcNow - _lastAuthAttempt.Value;
                if (timeSinceLastAttempt.TotalSeconds < AuthRetryDelaySeconds)
                {
                    var remainingSeconds = AuthRetryDelaySeconds - (int)timeSinceLastAttempt.TotalSeconds;
                    Console.WriteLine($"⏳ Waiting {remainingSeconds} more seconds before retrying authentication (to avoid rate limiting)...");
                    throw new InvalidOperationException($"Authentication retry cooldown: {remainingSeconds} seconds remaining. Previous attempt failed.");
                }
            }

            // Create a timeout token (30 seconds for authentication)
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                Console.WriteLine("🔐 Attempting MTProto authentication...");
                _lastAuthAttempt = DateTime.UtcNow;

            // Login to Telegram
            var myself = await _client.LoginUserIfNeeded();
            
            if (myself == null)
            {
                    _isAuthenticated = false;
                    _lastAuthAttempt = DateTime.UtcNow;
                throw new InvalidOperationException("Failed to authenticate with Telegram. Check your credentials.");
            }

            _isAuthenticated = true;
                _lastAuthAttempt = null; // Reset on success
            Console.WriteLine($"✅ MTProto authenticated as: {myself.username ?? myself.first_name} (ID: {myself.id})");
            
            // Notify that authentication was successful
            MtProtoAuthStore.NotifyAuthenticationSuccess();
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _isAuthenticated = false;
                _lastAuthAttempt = DateTime.UtcNow;
                Console.WriteLine($"⏱️ Authentication timed out. Will retry after {AuthRetryDelaySeconds} seconds.");
                throw new TimeoutException("MTProto authentication timed out. Check your network connection and Telegram server availability.");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _isAuthenticated = false;
                _lastAuthAttempt = DateTime.UtcNow;
                Console.WriteLine($"🌐 Network error during authentication: {ex.Message}");
                Console.WriteLine($"💡 Will retry after {AuthRetryDelaySeconds} seconds.");
                throw;
            }
            catch (Exception ex)
            {
                _isAuthenticated = false;
                _lastAuthAttempt = DateTime.UtcNow;
                Console.WriteLine($"❌ Authentication failed: {ex.Message}");
                Console.WriteLine($"❌ Exception type: {ex.GetType().FullName}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                // Check for specific Telegram errors
                if (ex.Message.Contains("PHONE_MIGRATE") || ex.Message.Contains("PHONE_CODE"))
                {
                    Console.WriteLine($"💡 This error usually means MTProto needs to be reconfigured. Please use /mtproto_setup.");
                    throw new InvalidOperationException("MTProto authentication failed. Please reconfigure MTProto using /mtproto_setup. Error: " + ex.Message, ex);
                }
                
                Console.WriteLine($"💡 Will retry after {AuthRetryDelaySeconds} seconds.");
                throw;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Resets authentication state to force a retry on next attempt
    /// </summary>
    public void ResetAuthentication()
    {
        _isAuthenticated = false;
        _lastAuthAttempt = null;
        Console.WriteLine("🔄 Authentication state reset. Next attempt will try to authenticate.");
    }

    public async Task<bool> IsContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a timeout token (15 seconds for contact check)
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

            await EnsureAuthenticatedAsync(timeoutCts.Token);

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
            try
            {
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
            }
            catch (Exception ex)
            {
                // Error getting contacts - this is an error, not "contact not found"
                Console.WriteLine($"❌ Error getting contacts list: {ex.Message}");
                throw new Exception($"خطا در دریافت لیست کانتکت‌ها: {ex.Message}", ex);
            }

            // If we reach here, contact check completed successfully but user is not in contacts
            Console.WriteLine($"ℹ️ User {recipientTelegramUserId} has not added sender account to their contacts (check completed successfully)");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var errorMsg = $"⏱️ بررسی کانتکت timeout شد. ممکن است به دلیل مشکلات شبکه یا عدم دسترسی به سرورهای Telegram باشد.";
            Console.WriteLine($"⏱️ Contact check timed out for user {recipientTelegramUserId}. This may be due to network issues or Telegram server unavailability.");
            Console.WriteLine($"💡 Tip: Check your internet connection and ensure Telegram servers are accessible.");
            throw new TimeoutException(errorMsg);
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"⏱️ Contact check timed out: {ex.Message}");
            throw new TimeoutException($"⏱️ بررسی کانتکت timeout شد: {ex.Message}");
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            var errorMsg = $"🌐 خطای شبکه در بررسی وضعیت کانتکت: {ex.Message}";
            Console.WriteLine($"🌐 Network error checking contact status: {ex.Message}");
            Console.WriteLine($"💡 Tip: Check your internet connection and firewall settings.");
            throw new Exception(errorMsg, ex);
        }
        catch (Exception ex)
        {
            var errorMsg = $"❌ خطا در بررسی وضعیت کانتکت: {ex.Message}";
            Console.WriteLine($"❌ Error checking contact status: {ex.Message}");
            Console.WriteLine($"💡 Error type: {ex.GetType().Name}");
            throw new Exception(errorMsg, ex);
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
            Console.WriteLine($"📤 SendPhotoWithTimerAsync called for user {recipientTelegramUserId}");
            Console.WriteLine($"🔐 Ensuring authentication...");
            await EnsureAuthenticatedAsync(cancellationToken);
            Console.WriteLine($"✅ Authentication successful");

            // Validate contact first - catch exceptions to distinguish errors from missing contact
            bool isContact;
            try
            {
                isContact = await IsContactAsync(recipientTelegramUserId, cancellationToken);
            }
            catch (Exception ex)
            {
                // If there's an error checking contact, return error message instead of "contact required"
                Console.WriteLine($"❌ Error checking contact status: {ex.Message}");
                return ContentDeliveryResult.Failure($"❌ خطا در بررسی وضعیت کانتکت: {ex.Message}");
            }

            if (!isContact)
            {
                return ContentDeliveryResult.Failure("❌ لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید");
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
                Console.WriteLine($"❌ Messages_SendMedia returned null");
                return ContentDeliveryResult.Failure("❌ Failed to send photo");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending photo: {ex.Message}");
            Console.WriteLine($"❌ Exception type: {ex.GetType().FullName}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message ?? "None"}");
            
            // Check for specific Telegram errors
            if (ex.Message.Contains("PHONE_MIGRATE") || ex.Message.Contains("PHONE_CODE") || ex.Message.Contains("not authenticated"))
            {
                Console.WriteLine($"💡 MTProto authentication issue detected. Please reconfigure using /mtproto_setup.");
                return ContentDeliveryResult.Failure($"❌ MTProto authentication required. Please configure using /mtproto_setup. Error: {ex.Message}");
            }
            
            return ContentDeliveryResult.Failure($"❌ خطا در ارسال عکس: {ex.Message}");
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

            // Validate contact first - catch exceptions to distinguish errors from missing contact
            bool isContact;
            try
            {
                Console.WriteLine($"🔍 Checking contact status for user {recipientTelegramUserId}...");
                isContact = await IsContactAsync(recipientTelegramUserId, cancellationToken);
                Console.WriteLine($"✅ Contact check result: {isContact}");
            }
            catch (Exception ex)
            {
                // If there's an error checking contact, return error message instead of "contact required"
                Console.WriteLine($"❌ Error checking contact status: {ex.Message}");
                Console.WriteLine($"❌ Exception type: {ex.GetType().FullName}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return ContentDeliveryResult.Failure($"❌ خطا در بررسی وضعیت کانتکت: {ex.Message}");
            }

            if (!isContact)
            {
                Console.WriteLine($"❌ User {recipientTelegramUserId} is not in contacts");
                return ContentDeliveryResult.Failure("❌ لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید");
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

    /// <summary>
    /// Checks if Telegram servers are reachable
    /// </summary>
    public async Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            Console.WriteLine("🔍 Checking connectivity to Telegram servers...");
            
            // Try to connect (this will fail fast if unreachable)
            var myself = await _client.LoginUserIfNeeded();
            
            if (myself != null)
            {
                Console.WriteLine("✅ Telegram servers are reachable");
                return true;
            }
            
            return false;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            Console.WriteLine($"❌ Cannot reach Telegram servers: {ex.Message}");
            Console.WriteLine($"💡 Possible causes:");
            Console.WriteLine($"   - Firewall blocking port 443");
            Console.WriteLine($"   - Network connectivity issues");
            Console.WriteLine($"   - ISP blocking Telegram servers");
            Console.WriteLine($"   - VPN required in your region");
            Console.WriteLine($"   - Telegram servers temporarily unavailable");
            return false;
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"⏱️ Connection to Telegram servers timed out: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Connectivity check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reinitializes the MTProto service with new credentials
    /// </summary>
    public async Task ReinitializeAsync(string apiId, string apiHash, string phoneNumber, string? sessionPath = null, CancellationToken cancellationToken = default)
    {
        await _reinitLock.WaitAsync(cancellationToken);
        try
        {
            Console.WriteLine("🔄 Reinitializing MTProto service with new credentials...");
            
            // Dispose old client and wait a bit to ensure file is released
            if (_client != null)
            {
                try
                {
                    await _client.DisposeAsync();
                    // Wait a bit to ensure file handles are released
                    await Task.Delay(500, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error disposing old client: {ex.Message}");
                }
            }
            
            // Update credentials
            _apiId = apiId ?? throw new ArgumentNullException(nameof(apiId));
            _apiHash = apiHash ?? throw new ArgumentNullException(nameof(apiHash));
            _phoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            _sessionPath = sessionPath;
            
            // Reset authentication state
            _isAuthenticated = false;
            _lastAuthAttempt = null;
            
            // Create new client with new credentials
            // Use a unique session path to avoid conflicts if multiple instances exist
            var finalSessionPath = _sessionPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(finalSessionPath)!);
            
            _client = new Client(Config);
            
            Console.WriteLine("✅ MTProto service reinitialized successfully");
        }
        finally
        {
            _reinitLock.Release();
        }
    }

    /// <summary>
    /// Attempts to authenticate with the current credentials
    /// </summary>
    public async Task<bool> TestAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            return _isAuthenticated;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Authentication test failed: {ex.Message}");
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            try
            {
                await _client.DisposeAsync();
                // Wait a bit to ensure file handles are released
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error disposing MTProto client: {ex.Message}");
            }
        }
        
        // Remove session file from active list
        if (!string.IsNullOrEmpty(_sessionPath))
        {
            lock (_sessionFileLock)
            {
                _activeSessionFiles.Remove(_sessionPath);
            }
        }
        
        _initLock?.Dispose();
        _reinitLock?.Dispose();
    }
}
