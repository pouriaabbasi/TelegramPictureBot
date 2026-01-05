using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IContentNotificationRepository _notificationRepository;
    private readonly IModelSubscriptionRepository _subscriptionRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITelegramBotService _telegramBotService;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        IContentNotificationRepository notificationRepository,
        IModelSubscriptionRepository subscriptionRepository,
        IPhotoRepository photoRepository,
        IUserRepository userRepository,
        ITelegramBotService telegramBotService,
        ILocalizationService localizationService,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _telegramBotService = telegramBotService ?? throw new ArgumentNullException(nameof(telegramBotService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<int> CreateNotificationsForNewContentAsync(
        Guid modelId, 
        Guid contentId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"📢 Creating notifications for new content {contentId} from model {modelId}");

            // Get all active subscribers of this model
            var subscriptions = await _subscriptionRepository.GetModelSubscriptionsAsync(modelId, cancellationToken);
            var activeSubscribers = subscriptions
                .Where(s => s.IsActive)
                .Select(s => s.UserId)
                .Distinct()
                .ToList();

            Console.WriteLine($"📢 Found {activeSubscribers.Count} active subscribers for model {modelId}");

            var createdCount = 0;

            foreach (var userId in activeSubscribers)
            {
                // Check if notification already exists
                var exists = await _notificationRepository.NotificationExistsAsync(userId, contentId, cancellationToken);
                if (exists)
                {
                    continue;
                }

                // Create notification
                var notification = new ContentNotification(contentId, modelId, userId);
                await _notificationRepository.AddAsync(notification, cancellationToken);
                createdCount++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"✅ Created {createdCount} notifications for content {contentId}");

            return createdCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating notifications for content {contentId}: {ex.Message}");
            throw;
        }
    }

    public async Task<(int Sent, int Failed)> SendPendingNotificationsAsync(
        int batchSize = 50, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"📤 Sending pending notifications in batches of {batchSize}");

            var sentCount = 0;
            var failedCount = 0;

            // Get pending notifications
            var pendingNotifications = await _notificationRepository.GetPendingNotificationsAsync(
                batchSize, cancellationToken);
            var notificationsList = pendingNotifications.ToList();

            if (!notificationsList.Any())
            {
                return (0, 0);
            }

            Console.WriteLine($"📤 Processing {notificationsList.Count} pending notifications");

            foreach (var notification in notificationsList)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("⚠️ Notification sending cancelled");
                    break;
                }

                try
                {
                    // Mark as sending
                    notification.MarkAsSending();
                    await _notificationRepository.UpdateAsync(notification, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Get user and content info
                    var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
                    var photo = await _photoRepository.GetByIdAsync(notification.ContentId, cancellationToken);

                    if (user == null || photo == null)
                    {
                        notification.MarkAsFailed("User or content not found");
                        await _notificationRepository.UpdateAsync(notification, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        failedCount++;
                        continue;
                    }

                    // Get localized message
                    var message = await _localizationService.GetStringAsync(
                        "notification.new_content", 
                        photo.Caption ?? "New content");

                    // Send notification via Telegram
                    await _telegramBotService.SendMessageAsync(
                        user.TelegramUserId, 
                        message, 
                        cancellationToken);

                    // Mark as sent
                    notification.MarkAsSent();
                    await _notificationRepository.UpdateAsync(notification, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    sentCount++;

                    // Delay to respect Telegram rate limits (1 second between messages)
                    await Task.Delay(1000, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error sending notification {notification.Id}: {ex.Message}");
                    
                    notification.MarkAsFailed(ex.Message);
                    await _notificationRepository.UpdateAsync(notification, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    failedCount++;
                }
            }

            Console.WriteLine($"✅ Sent {sentCount} notifications, {failedCount} failed");

            return (sentCount, failedCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in SendPendingNotificationsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<int> RetryFailedNotificationsAsync(
        int maxRetries = 3, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var failedNotifications = await _notificationRepository.GetRetryableNotificationsAsync(
                maxRetries, cancellationToken);
            var notificationsList = failedNotifications.ToList();

            foreach (var notification in notificationsList)
            {
                notification.ResetForRetry();
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (notificationsList.Count > 0)
            {
                Console.WriteLine($"🔄 Reset {notificationsList.Count} failed notifications for retry");
            }

            return notificationsList.Count;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in RetryFailedNotificationsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<(int Total, int Sent, int Failed, int Pending)> GetNotificationStatsAsync(
        Guid contentId, 
        CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.GetContentNotificationStatsAsync(contentId, cancellationToken);
    }
}
