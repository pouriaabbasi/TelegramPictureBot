using System.Collections.Concurrent;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Stores authentication credentials for MTProto during interactive authentication
/// </summary>
public static class MtProtoAuthStore
{
    private static readonly ConcurrentDictionary<string, string> _credentials = new();
    private static Func<long, Task>? _onVerificationCodeNeeded;
    private static Func<long, Task>? _on2FAPasswordNeeded;
    private static long? _currentChatId;
    
    /// <summary>
    /// Sets the chat ID for the current authentication session
    /// </summary>
    public static void SetCurrentChatId(long chatId)
    {
        _currentChatId = chatId;
    }
    
    /// <summary>
    /// Sets the callback to be called when verification code is needed
    /// </summary>
    public static void SetVerificationCodeCallback(Func<long, Task> callback)
    {
        _onVerificationCodeNeeded = callback;
    }
    
    /// <summary>
    /// Sets the callback to be called when 2FA password is needed
    /// </summary>
    public static void Set2FAPasswordCallback(Func<long, Task> callback)
    {
        _on2FAPasswordNeeded = callback;
    }
    
    /// <summary>
    /// Notifies that verification code is needed
    /// </summary>
    public static void NotifyVerificationCodeNeeded()
    {
        if (_currentChatId.HasValue && _onVerificationCodeNeeded != null)
        {
            // Fire and forget - don't block the authentication thread
            _ = Task.Run(async () =>
            {
                try
                {
                    await _onVerificationCodeNeeded(_currentChatId.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in verification code notification callback: {ex.Message}");
                }
            });
        }
    }
    
    /// <summary>
    /// Notifies that 2FA password is needed
    /// </summary>
    public static void Notify2FAPasswordNeeded()
    {
        if (_currentChatId.HasValue && _on2FAPasswordNeeded != null)
        {
            // Fire and forget - don't block the authentication thread
            _ = Task.Run(async () =>
            {
                try
                {
                    await _on2FAPasswordNeeded(_currentChatId.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in 2FA password notification callback: {ex.Message}");
                }
            });
        }
    }
    
    /// <summary>
    /// Sets the verification code sent to user's phone/app
    /// </summary>
    public static void SetVerificationCode(string code)
    {
        _credentials["verification_code"] = code;
        Console.WriteLine($"✅ Verification code stored: {code}");
    }
    
    /// <summary>
    /// Sets the 2FA password (if enabled)
    /// </summary>
    public static void Set2FAPassword(string password)
    {
        _credentials["password"] = password;
        Console.WriteLine($"✅ 2FA password stored");
    }
    
    /// <summary>
    /// Gets the verification code and removes it from store
    /// </summary>
    public static string? GetAndRemoveVerificationCode()
    {
        if (_credentials.TryRemove("verification_code", out var code))
        {
            Console.WriteLine($"📤 Retrieved verification code: {code}");
            return code;
        }
        return null;
    }
    
    /// <summary>
    /// Gets the 2FA password and removes it from store
    /// </summary>
    public static string? GetAndRemove2FAPassword()
    {
        if (_credentials.TryRemove("password", out var password))
        {
            Console.WriteLine($"📤 Retrieved 2FA password");
            return password;
        }
        return null;
    }
    
    /// <summary>
    /// Clears all stored credentials and callbacks
    /// </summary>
    public static void Clear()
    {
        _credentials.Clear();
        _currentChatId = null;
        _onVerificationCodeNeeded = null;
        _on2FAPasswordNeeded = null;
        Console.WriteLine("🧹 Cleared all stored credentials");
    }
}

