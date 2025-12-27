using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Background service for MTProto client (matching WTelegramClient working example)
/// </summary>
public sealed class MtProtoBackgroundService : BackgroundService
{
    public readonly WTelegram.Client Client;
    public TL.User? User => Client.User;
    public string ConfigNeeded = "connecting";

    private readonly IPlatformSettingsRepository _settingsRepo;
    private readonly ILogger<MtProtoBackgroundService> _logger;

    public MtProtoBackgroundService(
        IPlatformSettingsRepository settingsRepo,
        ILogger<MtProtoBackgroundService> logger)
    {
        _settingsRepo = settingsRepo;
        _logger = logger;
        
        WTelegram.Helpers.Log = (lvl, msg) => _logger.Log((LogLevel)lvl, msg);
        
        Client = new WTelegram.Client(what =>
        {
            // Synchronous config callback - must use .Result like the working example
            var value = _settingsRepo.GetValueAsync($"telegram:mtproto:{what}", default).Result;
            Console.WriteLine($"📋 Config callback: {what} = {(what == "api_hash" ? "***" : value ?? "null")}");
            return value;
        });
    }

    public override void Dispose()
    {
        Client.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var phoneNumber = await _settingsRepo.GetValueAsync("telegram:mtproto:phone_number", stoppingToken);
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                Console.WriteLine($"🔐 Starting login with phone: {phoneNumber}");
                ConfigNeeded = await DoLogin(phoneNumber);
            }
            else
            {
                Console.WriteLine("⚠️ No phone number configured. Waiting for web setup...");
                ConfigNeeded = "api_id"; // Start from the beginning
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MTProto initialization");
            ConfigNeeded = "error";
        }
    }

    public async Task<string?> DoLogin(string loginInfo)
    {
        try
        {
            Console.WriteLine($"🔐 DoLogin called with: {loginInfo}");
            var result = await Client.Login(loginInfo);
            ConfigNeeded = result ?? "authenticated";
            Console.WriteLine($"✅ Login result: {ConfigNeeded}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Login error: {ex.Message}");
            ConfigNeeded = "error";
            throw;
        }
    }
}

