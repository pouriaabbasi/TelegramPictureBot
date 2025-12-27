namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Defines the types of user states for tracking ongoing interactions
/// </summary>
public enum UserStateType
{
    /// <summary>
    /// No active state
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Waiting for premium media upload
    /// </summary>
    UploadingPremiumMedia = 1,
    
    /// <summary>
    /// Waiting for demo media upload
    /// </summary>
    UploadingDemoMedia = 2,
    
    /// <summary>
    /// Waiting for premium media price after upload
    /// </summary>
    SettingPremiumMediaPrice = 3,
    
    /// <summary>
    /// Waiting for premium media caption after upload
    /// </summary>
    SettingPremiumMediaCaption = 4,
    
    /// <summary>
    /// Waiting for caption edit input
    /// </summary>
    EditingCaption = 5,
    
    /// <summary>
    /// Waiting for price edit input
    /// </summary>
    EditingPrice = 6,
    
    /// <summary>
    /// Waiting for subscription plan input (price and duration)
    /// </summary>
    SettingSubscriptionPlan = 7,
    
    /// <summary>
    /// Admin editing a platform setting value
    /// </summary>
    EditingPlatformSetting = 8,
    
    /// <summary>
    /// Admin setting up MTProto - step 1: API ID
    /// </summary>
    MtProtoSetupApiId = 9,
    
    /// <summary>
    /// Admin setting up MTProto - step 2: API Hash
    /// </summary>
    MtProtoSetupApiHash = 10,
    
    /// <summary>
    /// Admin setting up MTProto - step 3: Phone Number
    /// </summary>
    MtProtoSetupPhoneNumber = 11
}

