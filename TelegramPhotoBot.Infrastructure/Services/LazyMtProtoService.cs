using System.IO;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Lazy wrapper for MTProto service that only initializes when actually needed
/// </summary>
public class LazyMtProtoService : IMtProtoService
{
    private readonly Func<IMtProtoService> _serviceFactory;
    private readonly object _lock = new object();
    private IMtProtoService? _service;
    private bool _isInitialized = false;

    public LazyMtProtoService(Func<IMtProtoService> serviceFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    }

    private IMtProtoService GetOrCreateService()
    {
        if (!_isInitialized)
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    try
                    {
                        _service = _serviceFactory();
                        _isInitialized = true;
                        Console.WriteLine("✅ MTProto service initialized on first use");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
                    {
                        // Credentials not configured - this is expected, don't log as error
                        Console.WriteLine("ℹ️ MTProto service not yet configured. Will be initialized when admin configures it.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Failed to initialize MTProto service: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        
        if (_service == null)
        {
            throw new InvalidOperationException("MTProto service is not configured. Please use /mtproto_setup to configure.");
        }
        
        return _service;
    }
    
    /// <summary>
    /// Gets the underlying MTProto service if it's initialized (for advanced operations like ResetAuthentication)
    /// </summary>
    public MtProtoService? GetUnderlyingService()
    {
        if (!_isInitialized || _service == null)
        {
            return null;
        }
        
        return _service as MtProtoService;
    }

    public Task<bool> IsContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = GetOrCreateService();
            return service.IsContactAsync(recipientTelegramUserId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Service not configured - return false and log
            Console.WriteLine("⚠️ MTProto service not configured. Please use /mtproto_setup to configure.");
            return Task.FromResult(false);
        }
    }

    public Task<ContentDeliveryResult> SendPhotoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = GetOrCreateService();
            return service.SendPhotoWithTimerAsync(recipientTelegramUserId, filePath, caption, selfDestructSeconds, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("⚠️ MTProto service not configured. Please use /mtproto_setup to configure.");
            return Task.FromResult(ContentDeliveryResult.Failure("❌ MTProto service is not configured. Please contact admin."));
        }
    }

    public Task<ContentDeliveryResult> SendVideoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = GetOrCreateService();
            return service.SendVideoWithTimerAsync(recipientTelegramUserId, filePath, caption, selfDestructSeconds, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("⚠️ MTProto service not configured. Please use /mtproto_setup to configure.");
            return Task.FromResult(ContentDeliveryResult.Failure("❌ MTProto service is not configured. Please contact admin."));
        }
    }

    public async Task ReinitializeAsync(string apiId, string apiHash, string phoneNumber, string? sessionPath = null, CancellationToken cancellationToken = default)
    {
        IMtProtoService? oldService = null;
        IMtProtoService newService;
        
        lock (_lock)
        {
            // If service is already initialized, mark it for disposal
            if (_service != null && _service is MtProtoService oldMtProtoService)
            {
                oldService = oldMtProtoService;
            }
            
            // Create new service with new credentials directly (bypass factory)
            var finalSessionPath = sessionPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(finalSessionPath)!);
            
            newService = new MtProtoService(apiId, apiHash, phoneNumber, finalSessionPath);
            _service = newService;
            _isInitialized = true;
        }
        
        // Dispose old service outside lock to avoid deadlock
        if (oldService != null && oldService is IAsyncDisposable disposable)
        {
            try
            {
                await disposable.DisposeAsync();
                // Wait a bit to ensure file handles are released
                await Task.Delay(500, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error disposing old MTProto service: {ex.Message}");
            }
        }
        
        // Don't await TestAuthenticationAsync here - it will wait for verification code and timeout
        // The caller (HandleMtProtoSetupPhoneNumberInputAsync) will start authentication in background
        Console.WriteLine("✅ MTProto service reinitialized. Authentication will be started separately.");
    }

    public Task<bool> TestAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var service = GetOrCreateService();
            return service.TestAuthenticationAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("⚠️ MTProto service not configured. Please use /mtproto_setup to configure.");
            return Task.FromResult(false);
        }
    }
}

