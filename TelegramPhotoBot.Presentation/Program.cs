using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Infrastructure.Data;
using TelegramPhotoBot.Presentation.Extensions;

namespace TelegramPhotoBot.Presentation;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddPresentationServices();

        // Add controllers if using webhooks
        builder.Services.AddControllers();

        var app = builder.Build();

        // Apply database migrations
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
            
            // Seed data disabled - uncomment below if needed
            // Seed platform settings from appsettings.json (one-time)
            // var settingsRepo = scope.ServiceProvider.GetRequiredService<TelegramPhotoBot.Application.Interfaces.Repositories.IPlatformSettingsRepository>();
            // var unitOfWork = scope.ServiceProvider.GetRequiredService<TelegramPhotoBot.Application.Interfaces.IUnitOfWork>();
            // var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            // var settingsSeeder = new PlatformSettingsSeeder(settingsRepo, unitOfWork, configuration);
            // await settingsSeeder.SeedAsync();
            
            // Seed test data for local development
            // await Data.TestDataSeeder.SeedAsync(dbContext);
        }

        // Configure the HTTP request pipeline
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // Telegram bot polling is started automatically by TelegramBotPollingService (BackgroundService)
        // No need to manually start it here

        await app.RunAsync();
    }
}
