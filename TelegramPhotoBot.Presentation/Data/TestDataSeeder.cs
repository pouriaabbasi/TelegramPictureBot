using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;
using TelegramPhotoBot.Infrastructure.Data;
using FileInfo = TelegramPhotoBot.Domain.ValueObjects.FileInfo;

namespace TelegramPhotoBot.Presentation.Data;

/// <summary>
/// Seeds test data for local development and testing
/// </summary>
public static class TestDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if data already exists
        if (await context.Users.AnyAsync())
        {
            return; // Data already seeded
        }

        // Create admin user
        var adminUser = new User(
            new TelegramUserId(123456789),
            "testadmin",
            "Test",
            "Admin",
            "en",
            false);
        adminUser.PromoteToAdmin();

        // Create a test model user
        var modelUser = new User(
            new TelegramUserId(987654321),
            "testmodel",
            "Test",
            "Model",
            "en",
            false);

        context.Users.AddRange(adminUser, modelUser);
        await context.SaveChangesAsync();
        
        // Create a Model entity for the model user
        var testModel = new Model(modelUser.Id, "Test Model", "A test content creator");
        testModel.Approve(adminUser.Id); // Approve the model with admin ID
        testModel.SetSubscriptionPricing(new TelegramStars(1000), 30);
        
        context.Models.Add(testModel);
        await context.SaveChangesAsync();
        
        // Link the model user to the model entity and promote to Model role
        modelUser.PromoteToModel(testModel.Id);
        await context.SaveChangesAsync();

        // Create test photos
        // For testing, we'll use paths relative to the application directory
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestPhotos");
        Directory.CreateDirectory(basePath);
        
        // Create placeholder image files if they don't exist
        for (int i = 1; i <= 3; i++)
        {
            var photoPath = Path.Combine(basePath, $"photo{i}.jpg");
            if (!System.IO.File.Exists(photoPath))
            {
                // Create a simple 1x1 pixel placeholder image
                CreatePlaceholderImage(photoPath);
            }
        }
        
        // Create premium photos for the model
        var photo1 = new Photo(
            new FileInfo("photo1.jpg", filePath: Path.Combine(basePath, "photo1.jpg")),
            sellerId: modelUser.Id,
            modelId: testModel.Id,
            price: new TelegramStars(500),
            type: PhotoType.Premium,
            caption: "Test Photo 1 - Premium Content");

        var photo2 = new Photo(
            new FileInfo("photo2.jpg", filePath: Path.Combine(basePath, "photo2.jpg")),
            sellerId: modelUser.Id,
            modelId: testModel.Id,
            price: new TelegramStars(750),
            type: PhotoType.Premium,
            caption: "Test Photo 2 - Premium Content");

        // Create a demo photo (free) for the model
        var photo3 = new Photo(
            new FileInfo("photo3.jpg", filePath: Path.Combine(basePath, "photo3.jpg")),
            sellerId: modelUser.Id,
            modelId: testModel.Id,
            price: new TelegramStars(0),
            type: PhotoType.Demo,
            caption: "Test Photo 3 - Demo/Preview");

        context.Photos.AddRange(photo1, photo2, photo3);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Test data seeded successfully!");
        Console.WriteLine($"   - Admin User ID: {adminUser.Id}");
        Console.WriteLine($"   - Model User ID: {modelUser.Id}");
        Console.WriteLine($"   - Model ID: {testModel.Id}");
        Console.WriteLine($"   - Photos created: 3");
        Console.WriteLine($"   - Test photos directory: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestPhotos")}");
    }
    
    private static void CreatePlaceholderImage(string filePath)
    {
        // Create a simple placeholder JPEG file
        // This is a minimal valid JPEG file (1x1 pixel, red)
        byte[] jpegData = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x03, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x02, 0x02, 0x02, 0x03,
            0x03, 0x03, 0x03, 0x04, 0x06, 0x04, 0x04, 0x04, 0x04, 0x04, 0x08, 0x06,
            0x06, 0x05, 0x06, 0x09, 0x08, 0x0A, 0x0A, 0x09, 0x08, 0x09, 0x09, 0x0A,
            0x0C, 0x0F, 0x0C, 0x0A, 0x0B, 0x0E, 0x0B, 0x09, 0x09, 0x0D, 0x11, 0x0D,
            0x0E, 0x0F, 0x10, 0x10, 0x11, 0x10, 0x0A, 0x0C, 0x12, 0x13, 0x12, 0x10,
            0x13, 0x0F, 0x10, 0x10, 0x10, 0xFF, 0xC9, 0x00, 0x0B, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xCC, 0x00, 0x06, 0x00, 0x10,
            0x10, 0x05, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00,
            0xD2, 0xCF, 0x20, 0xFF, 0xD9
        };
        
        System.IO.File.WriteAllBytes(filePath, jpegData);
        Console.WriteLine($"   - Created placeholder image: {filePath}");
    }
}

