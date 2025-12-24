using Microsoft.Extensions.Configuration;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Data;

/// <summary>
/// Seeds platform settings from appsettings.json to database (one-time migration)
/// </summary>
public class PlatformSettingsSeeder
{
    private readonly IPlatformSettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public PlatformSettingsSeeder(
        IPlatformSettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("🌱 Seeding platform settings from appsettings.json...");

        // Check if already seeded
        if (await _settingsRepository.ExistsAsync(PlatformSettings.Keys.MtProtoApiId))
        {
            Console.WriteLine("✅ Platform settings already seeded. Skipping...");
            return;
        }

        // Note: Bot token is NOT stored in database - it's required to start the bot
        // and must remain in appsettings.json for bootstrapping

        // MTProto / User API
        await SetIfNotNull(
            PlatformSettings.Keys.MtProtoApiId,
            _configuration["Telegram:MtProto:ApiId"],
            "Telegram API ID for MTProto User API",
            isSecret: false);

        await SetIfNotNull(
            PlatformSettings.Keys.MtProtoApiHash,
            _configuration["Telegram:MtProto:ApiHash"],
            "Telegram API Hash for MTProto User API",
            isSecret: true);

        await SetIfNotNull(
            PlatformSettings.Keys.MtProtoPhoneNumber,
            _configuration["Telegram:MtProto:PhoneNumber"],
            "Phone number for MTProto authentication",
            isSecret: false);

        // Platform Settings
        await _settingsRepository.SetValueAsync(
            PlatformSettings.Keys.PlatformName,
            "TelegramPhotoBot",
            "Platform display name");

        await _settingsRepository.SetValueAsync(
            PlatformSettings.Keys.PlatformDescription,
            "Premium Content Marketplace",
            "Platform description");

        await _settingsRepository.SetValueAsync(
            PlatformSettings.Keys.DefaultSelfDestructSeconds,
            "60",
            "Default self-destruct timer for media (seconds)");

        await _unitOfWork.SaveChangesAsync();

        Console.WriteLine("✅ Platform settings seeded successfully!");
    }

    private async Task SetIfNotNull(string key, string? value, string? description, bool isSecret = false)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            await _settingsRepository.SetValueAsync(key, value, description, isSecret);
            Console.WriteLine($"  ✓ {key} = {(isSecret ? "***" : value)}");
        }
        else
        {
            Console.WriteLine($"  ⚠️ {key} is not set in appsettings.json");
        }
    }
}

