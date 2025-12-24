using FluentAssertions;
using Moq;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Services;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;
using Xunit;

namespace TelegramPhotoBot.Tests.Services;

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

    [Fact]
    public async Task HasActiveSubscriptionAsync_WithActiveSubscription_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Subscription(
            userId,
            Guid.NewGuid(),
            new DateRange(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(25)),
            new TelegramStars(1000));

        _subscriptionRepositoryMock
            .Setup(r => r.GetActiveSubscriptionByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _service.HasActiveSubscriptionAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPurchasedPhotoAsync_WithCompletedPurchase_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();

        var purchase = new PurchasePhoto(userId, photoId, new TelegramStars(500));
        purchase.MarkPaymentCompleted("payment-123");

        _purchaseRepositoryMock
            .Setup(r => r.GetPhotoPurchaseAsync(userId, photoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase);

        // Act
        var result = await _service.HasPurchasedPhotoAsync(userId, photoId);

        // Assert
        result.Should().BeTrue();
    }
}

