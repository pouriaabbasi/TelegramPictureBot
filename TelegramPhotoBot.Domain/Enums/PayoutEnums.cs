namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Payout method for model settlements
/// </summary>
public enum PayoutMethod
{
    /// <summary>
    /// Bank transfer (IBAN)
    /// </summary>
    BankTransfer = 1,
    
    /// <summary>
    /// Card to card transfer
    /// </summary>
    CardToCard = 2,
    
    /// <summary>
    /// Cryptocurrency payment
    /// </summary>
    Crypto = 3,
    
    /// <summary>
    /// Cash payment
    /// </summary>
    Cash = 4,
    
    /// <summary>
    /// Other payment method
    /// </summary>
    Other = 99
}

/// <summary>
/// Status of a payout transaction
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Payout requested but not yet processed
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Admin is processing the payout
    /// </summary>
    Processing = 2,
    
    /// <summary>
    /// Payout completed successfully
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Payout failed
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Payout cancelled
    /// </summary>
    Cancelled = 5
}
