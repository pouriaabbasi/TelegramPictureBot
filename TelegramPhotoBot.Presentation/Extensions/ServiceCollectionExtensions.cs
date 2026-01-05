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
        services.AddScoped<IPhotoPurchaseService, PhotoPurchaseService>();
        services.AddScoped<IMtProtoAccessTokenService, MtProtoAccessTokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IContactVerificationService, ContactVerificationService>();
        services.AddScoped<ISingleModelModeService, SingleModelModeService>();
        services.AddScoped<IModelTermsService, ModelTermsService>();
        services.AddScoped<IRevenueAnalyticsService, RevenueAnalyticsService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<INotificationService, NotificationService>();

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
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        
        // Marketplace Repositories
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IModelSubscriptionRepository, ModelSubscriptionRepository>();
        services.AddScoped<IDemoAccessRepository, DemoAccessRepository>();
        services.AddScoped<IUserStateRepository, UserStateRepository>();
        services.AddScoped<IViewHistoryRepository, ViewHistoryRepository>();
        services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();
        services.AddScoped<IUserContactVerificationRepository, UserContactVerificationRepository>();
        services.AddScoped<IModelTermsAcceptanceRepository, ModelTermsAcceptanceRepository>();
        services.AddScoped<IModelPayoutRepository, ModelPayoutRepository>();
        services.AddScoped<IContentNotificationRepository, ContentNotificationRepository>();
        services.AddScoped<IPendingStarPaymentRepository, PendingStarPaymentRepository>();

        // Telegram Services - Bot Token MUST be in appsettings.json (required for bootstrapping)
        var botToken = configuration["Telegram:BotToken"] 
            ?? throw new InvalidOperationException("Telegram:BotToken is required in appsettings.json");
        
        services.AddSingleton<Telegram.Bot.ITelegramBotClient>(sp => new Telegram.Bot.TelegramBotClient(botToken));
        services.AddScoped<ITelegramBotService>(sp => new TelegramBotService(botToken));

        // MTProto Service - Lazy initialization (NOT a hosted service anymore)
        // Client will only be created on first actual use, preventing startup errors
        // when credentials are not yet configured
        services.AddSingleton<MtProtoBackgroundService>();
        services.AddSingleton<IMtProtoService>(sp => sp.GetRequiredService<MtProtoBackgroundService>());

        return services;
    }

    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Handlers
        services.AddScoped<Handlers.TelegramUpdateHandler>();
        services.AddScoped<Handlers.PaymentCallbackHandler>();

        // Background services
        services.AddHostedService<Services.TelegramBotPollingService>();
        services.AddHostedService<NotificationBackgroundService>();

        return services;
    }
}

