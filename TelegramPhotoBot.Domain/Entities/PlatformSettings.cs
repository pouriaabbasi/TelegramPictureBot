namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Stores platform configuration and sensitive credentials
/// </summary>
public class PlatformSettings : BaseEntity
{
    public string Key { get; private set; }
    public string Value { get; private set; }
    public string? Description { get; private set; }
    public bool IsEncrypted { get; private set; }
    public bool IsSecret { get; private set; } // Hide value in UI

    // EF Core constructor
    protected PlatformSettings() { }

    public PlatformSettings(string key, string value, string? description = null, bool isSecret = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        Key = key;
        Value = value ?? string.Empty;
        Description = description;
        IsSecret = isSecret;
        IsEncrypted = false; // Can implement encryption later
    }

    public void UpdateValue(string newValue)
    {
        Value = newValue ?? string.Empty;
        MarkAsUpdated();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        MarkAsUpdated();
    }

    // Common setting keys as constants
    public static class Keys
    {
        // Note: Bot token is NOT stored here - it must remain in appsettings.json
        // as it's required to bootstrap the bot before database access is available
        
        // MTProto / User API
        public const string MtProtoApiId = "telegram:mtproto:api_id";
        public const string MtProtoApiHash = "telegram:mtproto:api_hash";
        public const string MtProtoPhoneNumber = "telegram:mtproto:phone_number";
        public const string MtProtoSessionData = "telegram:mtproto:session_data";
        
        // Sender Account Info
        public const string SenderFirstName = "telegram:sender:first_name";
        public const string SenderLastName = "telegram:sender:last_name";
        public const string SenderUsername = "telegram:sender:username";
        
        // Platform Settings
        public const string PlatformName = "platform:name";
        public const string PlatformDescription = "platform:description";
        public const string DefaultSelfDestructSeconds = "platform:default_self_destruct_seconds";
        
        // Single Model Mode Settings
        public const string SingleModelMode = "platform:single_model_mode";
        public const string DefaultModelId = "platform:default_model_id";

        /// <summary>
        /// Determines if a setting key contains sensitive data
        /// </summary>
        public static bool IsSecretKey(string key)
        {
            return key switch
            {
                MtProtoApiHash => true,
                MtProtoSessionData => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets a human-readable description for a setting key
        /// </summary>
        public static string GetDescription(string key)
        {
            return key switch
            {
                MtProtoApiId => "Telegram API ID for MTProto User API",
                MtProtoApiHash => "Telegram API Hash for MTProto User API",
                MtProtoPhoneNumber => "Phone number for MTProto authentication",
                MtProtoSessionData => "MTProto session data (auto-generated)",
                SenderFirstName => "Sender account first name",
                SenderLastName => "Sender account last name",
                SenderUsername => "Sender account username",
                PlatformName => "Platform display name",
                PlatformDescription => "Platform description",
                DefaultSelfDestructSeconds => "Default self-destruct timer for media (seconds)",
                SingleModelMode => "Enable single model mode (true/false)",
                DefaultModelId => "Default model ID for single model mode",
                _ => "Platform setting"
            };
        }
    }
}

