using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramPhotoBot.Application.Interfaces;

namespace TelegramPhotoBot.Infrastructure.Services;

/// <summary>
/// Background service that periodically sends pending notifications in batches
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Check every minute

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationBackgroundService is starting");

        // Wait a bit before starting to allow app to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("NotificationBackgroundService tick");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    // Send pending notifications (batch of 50)
                    var (sent, failed) = await notificationService.SendPendingNotificationsAsync(
                        batchSize: 50, 
                        cancellationToken: stoppingToken);

                    if (sent > 0 || failed > 0)
                    {
                        _logger.LogInformation("Notification batch completed: {Sent} sent, {Failed} failed", 
                            sent, failed);
                    }

                    // Retry failed notifications (max 3 retries)
                    var retried = await notificationService.RetryFailedNotificationsAsync(
                        maxRetries: 3, 
                        cancellationToken: stoppingToken);

                    if (retried > 0)
                    {
                        _logger.LogInformation("Reset {Count} failed notifications for retry", retried);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationBackgroundService");
            }

            // Wait for next interval
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("NotificationBackgroundService is stopping");
    }
}
