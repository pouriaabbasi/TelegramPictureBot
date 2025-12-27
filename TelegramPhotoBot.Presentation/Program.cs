using Microsoft.EntityFrameworkCore;
using Serilog;
using TelegramPhotoBot.Infrastructure.Data;
using TelegramPhotoBot.Presentation.Extensions;

namespace TelegramPhotoBot.Presentation;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build())
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "Logs/telegrambot-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Telegram Photo Bot application...");

        var builder = WebApplication.CreateBuilder(args);

            // Use Serilog for logging
            builder.Host.UseSerilog();

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

        // Configure the HTTP request pipeline (disable HTTPS for local testing)
        // app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // Telegram bot polling is started automatically by TelegramBotPollingService (BackgroundService)
        // No need to manually start it here

        Log.Information("Application started successfully");

        await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
