using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Tests.Utilities;

public static class TestDataBuilder
{
    public static User CreateTestUser(long telegramUserId = 123456789)
    {
        return new User(
            new TelegramUserId(telegramUserId),
            "testuser",
            "Test",
            "User",
            "en",
            false);
    }

    public static SubscriptionPlan CreateTestSubscriptionPlan(Guid adminId, long price = 1000, int durationDays = 30)
    {
        return new SubscriptionPlan(
            "Test Plan",
            "Test subscription plan",
            new TelegramStars(price),
            durationDays,
            adminId);
    }

    public static Photo CreateTestPhoto(Guid sellerId, long price = 500, string? filePath = null)
    {
        return new Photo(
            new FileInfo("test.jpg", filePath: filePath ?? "/test/test.jpg"),
            sellerId,
            new TelegramStars(price),
            "Test photo");
    }

    public static Subscription CreateTestSubscription(
        Guid userId,
        Guid planId,
        int daysRemaining = 25)
    {
        return new Subscription(
            userId,
            planId,
            new DateRange(
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(daysRemaining)),
            new TelegramStars(1000));
    }

    public static PurchasePhoto CreateTestPurchasePhoto(
        Guid userId,
        Guid photoId,
        long amount = 500,
        bool paymentCompleted = false)
    {
        var purchase = new PurchasePhoto(userId, photoId, new TelegramStars(amount));
        
        if (paymentCompleted)
        {
            purchase.MarkPaymentCompleted($"payment-{Guid.NewGuid()}");
        }

        return purchase;
    }
}

