using System.Collections.Concurrent;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Stores authentication credentials for MTProto during interactive authentication
/// </summary>
public static class MtProtoAuthStore
{
    private static readonly ConcurrentDictionary<string, string> _credentials = new();
    
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
    /// Clears all stored credentials
    /// </summary>
    public static void Clear()
    {
        _credentials.Clear();
        Console.WriteLine("🧹 Cleared all stored credentials");
    }
}

