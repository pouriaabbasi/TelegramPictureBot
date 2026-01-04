namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for localizing bot messages
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current bot language from platform settings
    /// </summary>
    Task<Domain.Enums.BotLanguage> GetBotLanguageAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a localized string by key
    /// </summary>
    Task<string> GetStringAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a localized string by key with parameters
    /// </summary>
    Task<string> GetStringAsync(string key, params object[] args);
    
    /// <summary>
    /// Sets the bot language in platform settings
    /// </summary>
    Task SetBotLanguageAsync(Domain.Enums.BotLanguage language, CancellationToken cancellationToken = default);
}

