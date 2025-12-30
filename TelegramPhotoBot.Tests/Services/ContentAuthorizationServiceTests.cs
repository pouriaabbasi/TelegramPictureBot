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
    private readonly Mock<IPurchaseRepository> _purchaseRepositoryMock;
    private readonly Mock<IModelSubscriptionService> _modelSubscriptionServiceMock;
    private readonly Mock<IPhotoRepository> _photoRepositoryMock;
    private readonly ContentAuthorizationService _service;

    public ContentAuthorizationServiceTests()
    {
        _purchaseRepositoryMock = new Mock<IPurchaseRepository>();
        _modelSubscriptionServiceMock = new Mock<IModelSubscriptionService>();
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _service = new ContentAuthorizationService(
            _purchaseRepositoryMock.Object,
            _modelSubscriptionServiceMock.Object,
            _photoRepositoryMock.Object);
    }

    [Fact]
    public async Task HasActiveSubscriptionAsync_AlwaysReturnsFalse_PlatformSubscriptionsDeprecated()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.HasActiveSubscriptionAsync(userId);

        // Assert
        result.Should().BeFalse();
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
