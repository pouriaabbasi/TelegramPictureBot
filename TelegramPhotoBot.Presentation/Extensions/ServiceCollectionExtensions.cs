using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Application.Services;
using TelegramPhotoBot.Infrastructure.Data;
using TelegramPhotoBot.Infrastructure.Repositories;
using TelegramPhotoBot.Infrastructure.Services;

namespace TelegramPhotoBot.Presentation.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Core Application Services
        services.AddScoped<IContentAuthorizationService, ContentAuthorizationService>();
        services.AddScoped<IPaymentVerificationService, PaymentVerificationService>();
        services.AddScoped<IContentDeliveryService, ContentDeliveryService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPhotoPurchaseService, PhotoPurchaseService>();
        services.AddScoped<IUserService, UserService>();

        // Marketplace Services
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<IModelDiscoveryService, ModelDiscoveryService>();
        services.AddScoped<IModelSubscriptionService, ModelSubscriptionService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        
        // Marketplace Repositories
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IModelSubscriptionRepository, ModelSubscriptionRepository>();
        services.AddScoped<IDemoAccessRepository, DemoAccessRepository>();
        services.AddScoped<IUserStateRepository, UserStateRepository>();
        services.AddScoped<IViewHistoryRepository, ViewHistoryRepository>();
        services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();

        // Telegram Services - Bot Token MUST be in appsettings.json (required for bootstrapping)
        var botToken = configuration["Telegram:BotToken"] 
            ?? throw new InvalidOperationException("Telegram:BotToken is required in appsettings.json");
        
        services.AddSingleton<Telegram.Bot.ITelegramBotClient>(sp => new Telegram.Bot.TelegramBotClient(botToken));
        services.AddScoped<ITelegramBotService>(sp => new TelegramBotService(botToken));

        // MTProto Service - Singleton to maintain persistent connection
        // Credentials loaded from database at initialization (after bot is running)
        services.AddSingleton<IMtProtoService>(sp =>
        {
            using var scope = sp.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<IPlatformSettingsRepository>();
            
            var apiId = settingsRepo.GetValueAsync(TelegramPhotoBot.Domain.Entities.PlatformSettings.Keys.MtProtoApiId).Result
                       ?? configuration["Telegram:MtProto:ApiId"]
                       ?? throw new InvalidOperationException("MTProto ApiId not found");
            
            var apiHash = settingsRepo.GetValueAsync(TelegramPhotoBot.Domain.Entities.PlatformSettings.Keys.MtProtoApiHash).Result
                         ?? configuration["Telegram:MtProto:ApiHash"]
                         ?? throw new InvalidOperationException("MTProto ApiHash not found");
            
            var phoneNumber = settingsRepo.GetValueAsync(TelegramPhotoBot.Domain.Entities.PlatformSettings.Keys.MtProtoPhoneNumber).Result
                             ?? configuration["Telegram:MtProto:PhoneNumber"]
                             ?? throw new InvalidOperationException("MTProto PhoneNumber not found");
            
            var sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(sessionPath)!);
            
            Console.WriteLine($"📱 Initializing MTProto with phone: {phoneNumber}");
            return new MtProtoService(apiId, apiHash, phoneNumber, sessionPath);
        });

        return services;
    }

    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Handlers
        services.AddScoped<Handlers.TelegramUpdateHandler>();
        services.AddScoped<Handlers.PaymentCallbackHandler>();

        // Background service for bot polling
        services.AddHostedService<Services.TelegramBotPollingService>();

        return services;
    }
}

