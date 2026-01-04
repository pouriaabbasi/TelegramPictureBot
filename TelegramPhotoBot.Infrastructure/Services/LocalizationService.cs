using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    private const string LanguageSettingKey = "platform:bot_language";
    private const string DefaultLanguage = "Persian"; // Default to Persian
    
    // Dictionary of all localized strings
    private static readonly Dictionary<string, Dictionary<BotLanguage, string>> _strings = new()
    {
        // Main Menu
        ["menu.welcome"] = new()
        {
            { BotLanguage.Persian, "👋 به بات خوش آمدید!" },
            { BotLanguage.English, "👋 Welcome to the bot!" }
        },
        ["menu.browse_models"] = new()
        {
            { BotLanguage.Persian, "🔍 Browse Models" },
            { BotLanguage.English, "🔍 Browse Models" }
        },
        ["menu.my_subscriptions"] = new()
        {
            { BotLanguage.Persian, "💎 My Subscriptions" },
            { BotLanguage.English, "💎 My Subscriptions" }
        },
        ["menu.my_content"] = new()
        {
            { BotLanguage.Persian, "📁 My Content" },
            { BotLanguage.English, "📁 My Content" }
        },
        ["menu.become_model"] = new()
        {
            { BotLanguage.Persian, "🎭 Become a Model" },
            { BotLanguage.English, "🎭 Become a Model" }
        },
        ["menu.model_dashboard"] = new()
        {
            { BotLanguage.Persian, "📊 Model Dashboard" },
            { BotLanguage.English, "📊 Model Dashboard" }
        },
        ["menu.admin_panel"] = new()
        {
            { BotLanguage.Persian, "🛡️ Admin Panel" },
            { BotLanguage.English, "🛡️ Admin Panel" }
        },
        ["menu.back"] = new()
        {
            { BotLanguage.Persian, "« Back to Main Menu" },
            { BotLanguage.English, "« Back to Main Menu" }
        },
        
        // Common Messages
        ["common.error"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در پردازش درخواست شما. لطفاً دوباره تلاش کنید." },
            { BotLanguage.English, "❌ Error processing your request. Please try again." }
        },
        ["common.not_found"] = new()
        {
            { BotLanguage.Persian, "❌ مورد یافت نشد." },
            { BotLanguage.English, "❌ Not found." }
        },
        ["common.success"] = new()
        {
            { BotLanguage.Persian, "✅ با موفقیت انجام شد." },
            { BotLanguage.English, "✅ Successfully completed." }
        },
        
        // Model Registration
        ["model.registration.rejected"] = new()
        {
            { BotLanguage.Persian, "درخواست ثبت‌نام شما رد شده است." },
            { BotLanguage.English, "Your registration request has been rejected." }
        },
        ["model.registration.submit_new"] = new()
        {
            { BotLanguage.Persian, "✅ Submit New Application" },
            { BotLanguage.English, "✅ Submit New Application" }
        },
        
        // Admin Settings
        ["admin.settings.language"] = new()
        {
            { BotLanguage.Persian, "🌐 Bot Language" },
            { BotLanguage.English, "🌐 Bot Language" }
        },
        ["admin.settings.language.current"] = new()
        {
            { BotLanguage.Persian, "زبان فعلی بات: {0}" },
            { BotLanguage.English, "Current bot language: {0}" }
        },
        ["admin.settings.language.select"] = new()
        {
            { BotLanguage.Persian, "لطفاً زبان مورد نظر را انتخاب کنید:" },
            { BotLanguage.English, "Please select the language:" }
        },
        ["admin.settings.language.persian"] = new()
        {
            { BotLanguage.Persian, "🇮🇷 فارسی" },
            { BotLanguage.English, "🇮🇷 Persian" }
        },
        ["admin.settings.language.english"] = new()
        {
            { BotLanguage.Persian, "🇬🇧 English" },
            { BotLanguage.English, "🇬🇧 English" }
        },
        ["admin.settings.language.updated"] = new()
        {
            { BotLanguage.Persian, "✅ زبان بات با موفقیت تغییر کرد." },
            { BotLanguage.English, "✅ Bot language updated successfully." }
        }
    };
    
    public LocalizationService(
        IPlatformSettingsRepository platformSettingsRepository,
        IUnitOfWork unitOfWork)
    {
        _platformSettingsRepository = platformSettingsRepository ?? throw new ArgumentNullException(nameof(platformSettingsRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }
    
    public async Task<BotLanguage> GetBotLanguageAsync(CancellationToken cancellationToken = default)
    {
        var languageValue = await _platformSettingsRepository.GetValueAsync(LanguageSettingKey, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(languageValue))
        {
            // Default to Persian if not set
            return BotLanguage.Persian;
        }
        
        return Enum.TryParse<BotLanguage>(languageValue, out var language) 
            ? language 
            : BotLanguage.Persian;
    }
    
    public async Task<string> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var language = await GetBotLanguageAsync(cancellationToken);
        return GetString(key, language);
    }
    
    public async Task<string> GetStringAsync(string key, params object[] args)
    {
        var language = await GetBotLanguageAsync();
        var template = GetString(key, language);
        return args.Length > 0 ? string.Format(template, args) : template;
    }
    
    private string GetString(string key, BotLanguage language)
    {
        if (_strings.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(language, out var text))
            {
                return text;
            }
            
            // Fallback to Persian if translation not found
            if (translations.TryGetValue(BotLanguage.Persian, out var fallback))
            {
                return fallback;
            }
        }
        
        // Return key if translation not found
        return key;
    }
    
    public async Task SetBotLanguageAsync(BotLanguage language, CancellationToken cancellationToken = default)
    {
        await _platformSettingsRepository.SetValueAsync(
            LanguageSettingKey,
            language.ToString(),
            "Bot language (Persian or English)",
            isSecret: false,
            cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

