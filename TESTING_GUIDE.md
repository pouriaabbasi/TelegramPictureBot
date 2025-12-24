# Testing Guide for Telegram Photo Bot

## Overview

This guide covers different testing approaches for the Telegram Photo Bot system:
1. **Unit Tests** - Test individual components in isolation
2. **Integration Tests** - Test component interactions
3. **Manual Testing** - Test with actual Telegram APIs
4. **End-to-End Testing** - Test complete flows

---

## Part 1: Unit Testing Setup

### Prerequisites

Create a test project:
```bash
dotnet new xunit -n TelegramPhotoBot.Tests
dotnet add TelegramPhotoBot.Tests reference TelegramPhotoBot.Application
dotnet add TelegramPhotoBot.Tests reference TelegramPhotoBot.Domain
dotnet add TelegramPhotoBot.Tests package Moq
dotnet add TelegramPhotoBot.Tests package FluentAssertions
dotnet add TelegramPhotoBot.Tests package Microsoft.EntityFrameworkCore.InMemory
```

### Example Unit Test: ContentAuthorizationService

```csharp
using FluentAssertions;
using Moq;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Services;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;
using Xunit;

namespace TelegramPhotoBot.Tests.Application.Services;

public class ContentAuthorizationServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryMock;
    private readonly Mock<IPurchaseRepository> _purchaseRepositoryMock;
    private readonly ContentAuthorizationService _service;

    public ContentAuthorizationServiceTests()
    {
        _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
        _purchaseRepositoryMock = new Mock<IPurchaseRepository>();
        _service = new ContentAuthorizationService(
            _subscriptionRepositoryMock.Object,
            _purchaseRepositoryMock.Object);
    }

    [Fact]
    public async Task CheckPhotoAccessAsync_WithActiveSubscription_ReturnsGranted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();
        
        var subscription = new Subscription(
            userId,
            Guid.NewGuid(),
            new DateRange(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(25)),
            new TelegramStars(1000));

        _subscriptionRepositoryMock
            .Setup(r => r.GetActiveSubscriptionByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _service.CheckPhotoAccessAsync(userId, photoId);

        // Assert
        result.HasAccess.Should().BeTrue();
        result.AccessType.Should().Be(ContentAccessType.Subscription);
    }

    [Fact]
    public async Task CheckPhotoAccessAsync_WithPurchasedPhoto_ReturnsGranted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();

        _subscriptionRepositoryMock
            .Setup(r => r.GetActiveSubscriptionByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var purchase = new PurchasePhoto(userId, photoId, new TelegramStars(500));
        purchase.MarkPaymentCompleted("payment-123");

        _purchaseRepositoryMock
            .Setup(r => r.GetPhotoPurchaseAsync(userId, photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase);

        // Act
        var result = await _service.CheckPhotoAccessAsync(userId, photoId);

        // Assert
        result.HasAccess.Should().BeTrue();
        result.AccessType.Should().Be(ContentAccessType.Purchase);
    }

    [Fact]
    public async Task CheckPhotoAccessAsync_NoAccess_ReturnsDenied()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();

        _subscriptionRepositoryMock
            .Setup(r => r.GetActiveSubscriptionByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        _purchaseRepositoryMock
            .Setup(r => r.GetPhotoPurchaseAsync(userId, photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchasePhoto?)null);

        // Act
        var result = await _service.CheckPhotoAccessAsync(userId, photoId);

        // Assert
        result.HasAccess.Should().BeFalse();
        result.Reason.Should().NotBeNullOrEmpty();
    }
}
```

### Example Unit Test: PaymentVerificationService

```csharp
public class PaymentVerificationServiceTests
{
    private readonly Mock<IPurchaseRepository> _purchaseRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PaymentVerificationService _service;

    public PaymentVerificationServiceTests()
    {
        _purchaseRepositoryMock = new Mock<IPurchaseRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new PaymentVerificationService(
            _purchaseRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task VerifyPaymentAsync_DuplicatePayment_ReturnsFailure()
    {
        // Arrange
        var paymentId = "payment-123";
        var purchase = new PurchasePhoto(Guid.NewGuid(), Guid.NewGuid(), new TelegramStars(500));
        purchase.MarkPaymentCompleted(paymentId);

        _purchaseRepositoryMock
            .Setup(r => r.GetByTelegramPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase);

        var request = new PaymentVerificationRequest
        {
            TelegramPaymentId = paymentId,
            PurchaseId = purchase.Id,
            TelegramUserId = purchase.UserId,
            Amount = 500,
            Currency = "XTR"
        };

        // Act
        var result = await _service.VerifyPaymentAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already been processed");
    }

    [Fact]
    public async Task VerifyPaymentAsync_ValidPayment_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();
        var purchase = new PurchasePhoto(userId, photoId, new TelegramStars(500));
        var paymentId = "payment-123";

        _purchaseRepositoryMock
            .Setup(r => r.GetByTelegramPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Purchase?)null);

        _purchaseRepositoryMock
            .Setup(r => r.GetByIdAsync(purchase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new PaymentVerificationRequest
        {
            TelegramPaymentId = paymentId,
            PurchaseId = purchase.Id,
            TelegramUserId = userId,
            Amount = 500,
            Currency = "XTR"
        };

        // Act
        var result = await _service.VerifyPaymentAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        purchase.IsPaymentCompleted().Should().BeTrue();
    }
}
```

---

## Part 2: Integration Testing

### Setup Integration Test Project

```bash
dotnet new xunit -n TelegramPhotoBot.IntegrationTests
dotnet add TelegramPhotoBot.IntegrationTests reference TelegramPhotoBot.Infrastructure
dotnet add TelegramPhotoBot.IntegrationTests reference TelegramPhotoBot.Application
dotnet add TelegramPhotoBot.IntegrationTests package Microsoft.EntityFrameworkCore.InMemory
dotnet add TelegramPhotoBot.IntegrationTests package FluentAssertions
```

### Example Integration Test: Repository Tests

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;
using TelegramPhotoBot.Infrastructure.Data;
using TelegramPhotoBot.Infrastructure.Repositories;
using Xunit;

namespace TelegramPhotoBot.IntegrationTests.Repositories;

public class SubscriptionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SubscriptionRepository _repository;

    public SubscriptionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new SubscriptionRepository(_context);
    }

    [Fact]
    public async Task GetActiveSubscriptionByUserIdAsync_WithActiveSubscription_ReturnsSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var plan = new SubscriptionPlan(
            "Premium",
            "Premium plan",
            new TelegramStars(1000),
            30,
            Guid.NewGuid());

        var subscription = new Subscription(
            userId,
            plan.Id,
            new DateRange(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(25)),
            new TelegramStars(1000));

        _context.Set<SubscriptionPlan>().Add(plan);
        _context.Set<Subscription>().Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSubscriptionByUserIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive().Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

---

## Part 3: Manual Testing Guide

### Step 1: Setup Test Environment

1. **Create a test Telegram bot:**
   - Go to [@BotFather](https://t.me/BotFather) on Telegram
   - Send `/newbot` command
   - Follow instructions to create a bot
   - Save the bot token

2. **Get Telegram API credentials:**
   - Go to [my.telegram.org](https://my.telegram.org)
   - Log in with your phone number
   - Go to "API development tools"
   - Create an application
   - Save `api_id` and `api_hash`

3. **Configure appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=test.db"
  },
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN",
    "MtProto": {
      "ApiId": "YOUR_API_ID",
      "ApiHash": "YOUR_API_HASH",
      "PhoneNumber": "+1234567890"
    }
  }
}
```

### Step 2: Test Database Setup

```csharp
// Create a test data seeder
public class TestDataSeeder
{
    public static async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Create test admin user
        var adminUser = new User(
            new TelegramUserId(123456789),
            "testadmin",
            "Test",
            "Admin",
            "en",
            false);

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Create subscription plan
        var plan = new SubscriptionPlan(
            "Test Premium",
            "Test premium subscription",
            new TelegramStars(1000),
            30,
            adminUser.Id);

        context.SubscriptionPlans.Add(plan);
        await context.SaveChangesAsync();

        // Create test photo
        var photo = new Photo(
            new FileInfo("test-photo.jpg", filePath: "/test/photos/test-photo.jpg"),
            adminUser.Id,
            new TelegramStars(500),
            "Test photo caption");

        context.Photos.Add(photo);
        await context.SaveChangesAsync();
    }
}
```

### Step 3: Test Scenarios

#### Scenario 1: Test User Registration

```csharp
// Test: User sends /start command
// Expected: User is created in database

// Manual test:
// 1. Open Telegram
// 2. Find your bot
// 3. Send /start
// 4. Check database: User should be created
```

#### Scenario 2: Test Subscription Purchase

```csharp
// Test: User purchases subscription
// Expected: Subscription created, invoice sent

// Manual test:
// 1. Send /subscriptions command
// 2. Select a subscription plan
// 3. Complete payment with Telegram Stars
// 4. Check database: 
//    - Purchase should be created
//    - Subscription should be created
//    - Payment status should be "Completed"
```

#### Scenario 3: Test Photo Purchase

```csharp
// Test: User purchases a photo
// Expected: Purchase created, photo delivered

// Manual test:
// 1. Send /photos command
// 2. Select a photo to purchase
// 3. Complete payment
// 4. Verify:
//    - Purchase created in database
//    - Payment verified
//    - Photo sent via MTProto (if contact added)
```

#### Scenario 4: Test Content Access

```csharp
// Test: User requests photo with active subscription
// Expected: Photo delivered without payment

// Manual test:
// 1. Ensure user has active subscription
// 2. Request a photo
// 3. Verify photo is delivered
// 4. No payment should be required
```

#### Scenario 5: Test Contact Validation

```csharp
// Test: User requests photo without adding sender to contacts
// Expected: Error message, photo not sent

// Manual test:
// 1. Purchase a photo
// 2. Don't add sender account to contacts
// 3. Verify error message: "Please add this account to your contacts first"
// 4. Add sender to contacts
// 5. Request photo again
// 6. Verify photo is delivered
```

#### Scenario 6: Test Duplicate Payment Prevention

```csharp
// Test: Same payment callback received twice
// Expected: Second callback ignored

// Manual test:
// 1. Complete a payment
// 2. Manually trigger payment callback again with same payment ID
// 3. Verify: Error message about duplicate payment
// 4. Check database: Only one purchase record exists
```

---

## Part 4: Test Utilities

### Mock Telegram Services for Testing

```csharp
public class MockTelegramBotService : ITelegramBotService
{
    public List<(long chatId, string message)> SentMessages { get; } = new();
    public List<CreateInvoiceRequest> CreatedInvoices { get; } = new();

    public Task<bool> SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((chatId, message));
        return Task.FromResult(true);
    }

    public Task<string?> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        CreatedInvoices.Add(request);
        return Task.FromResult("invoice-123");
    }

    public Task<bool> AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> VerifyPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

public class MockMtProtoService : IMtProtoService
{
    public bool IsContactResult { get; set; } = true;
    public List<(long userId, string filePath, int timer)> SentPhotos { get; } = new();

    public Task<bool> IsContactAsync(long recipientTelegramUserId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsContactResult);
    }

    public Task<ContentDeliveryResult> SendPhotoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        SentPhotos.Add((recipientTelegramUserId, filePath, selfDestructSeconds));
        return Task.FromResult(ContentDeliveryResult.Success());
    }

    public Task<ContentDeliveryResult> SendVideoWithTimerAsync(
        long recipientTelegramUserId,
        string filePath,
        string? caption,
        int selfDestructSeconds,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ContentDeliveryResult.Success());
    }
}
```

### Test Data Builder

```csharp
public class TestDataBuilder
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

    public static SubscriptionPlan CreateTestSubscriptionPlan(Guid adminId)
    {
        return new SubscriptionPlan(
            "Test Plan",
            "Test subscription plan",
            new TelegramStars(1000),
            30,
            adminId);
    }

    public static Photo CreateTestPhoto(Guid sellerId, long price = 500)
    {
        return new Photo(
            new FileInfo("test.jpg", filePath: "/test/test.jpg"),
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
}
```

---

## Part 5: Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test TelegramPhotoBot.Tests
dotnet test TelegramPhotoBot.IntegrationTests
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~ContentAuthorizationServiceTests"
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Part 6: Test Checklist

### Unit Tests
- [ ] ContentAuthorizationService - All scenarios
- [ ] PaymentVerificationService - Valid and invalid payments
- [ ] SubscriptionService - Create and retrieve
- [ ] PhotoPurchaseService - Create purchase
- [ ] UserService - Get or create user

### Integration Tests
- [ ] Repository operations
- [ ] Database transactions
- [ ] Entity relationships

### Manual Tests
- [ ] Bot responds to /start
- [ ] User registration works
- [ ] Subscription purchase flow
- [ ] Photo purchase flow
- [ ] Payment verification
- [ ] Content delivery
- [ ] Contact validation
- [ ] Duplicate payment prevention
- [ ] Error handling

---

## Part 7: Debugging Tips

### Enable Logging
```csharp
// In Program.cs
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

### Database Inspection
```csharp
// Add to test to inspect database state
var users = await context.Users.ToListAsync();
var purchases = await context.Purchases.ToListAsync();
```

### Mock Verification
```csharp
// Verify mock was called
_mockRepository.Verify(
    r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
    Times.Once);
```

---

## Quick Start Testing

1. **Create test projects:**
   ```bash
   dotnet new xunit -n TelegramPhotoBot.Tests
   dotnet sln add TelegramPhotoBot.Tests
   ```

2. **Add test files from examples above**

3. **Run tests:**
   ```bash
   dotnet test
   ```

4. **For manual testing:**
   - Configure appsettings.json
   - Run the application
   - Test with Telegram bot

