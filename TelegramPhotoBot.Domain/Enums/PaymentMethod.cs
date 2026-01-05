namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Payment methods supported by the platform
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Test/Manual payment (for development)
    /// </summary>
    Manual = 0,
    
    /// <summary>
    /// Telegram Invoice API payment
    /// </summary>
    TelegramInvoice = 1,
    
    /// <summary>
    /// Star Reaction payment (user sends stars via message reaction)
    /// </summary>
    StarReaction = 2
}
