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

    public static Photo CreateTestPhoto(Guid sellerId, Guid modelId, long price = 500, string? filePath = null)
    {
        return new Photo(
            new TelegramPhotoBot.Domain.ValueObjects.FileInfo("test.jpg", filePath: filePath ?? "/test/test.jpg"),
            sellerId,
            modelId,
            new TelegramStars(price),
            Domain.Enums.PhotoType.Premium,
            "Test photo");
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

