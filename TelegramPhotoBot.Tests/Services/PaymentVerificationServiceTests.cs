using FluentAssertions;
using Moq;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Services;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;
using Xunit;

namespace TelegramPhotoBot.Tests.Services;

public class PaymentVerificationServiceTests
{
    private readonly Mock<IPurchaseRepository> _purchaseRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PaymentVerificationService _service;

    public PaymentVerificationServiceTests()
    {
        _purchaseRepositoryMock = new Mock<IPurchaseRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new PaymentVerificationService(
            _purchaseRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task VerifyPaymentAsync_DuplicatePayment_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();
        var paymentId = "payment-123";
        var purchase = new PurchasePhoto(userId, photoId, new TelegramStars(500));
        purchase.MarkPaymentCompleted(paymentId);

        // Create a mock user
        var user = new User(new TelegramUserId(123456789), "testuser");
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _purchaseRepositoryMock
            .Setup(r => r.GetByTelegramPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase);

        var request = new PaymentVerificationRequest
        {
            TelegramPaymentId = paymentId,
            PurchaseId = purchase.Id,
            TelegramUserId = 123456789,
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

        // Create a mock user
        var user = new User(new TelegramUserId(987654321), "testuser");
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

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
            TelegramUserId = 987654321,
            Amount = 500,
            Currency = "XTR"
        };

        // Act
        var result = await _service.VerifyPaymentAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        purchase.IsPaymentCompleted().Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPaymentAsync_InvalidCurrency_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();
        var purchase = new PurchasePhoto(userId, photoId, new TelegramStars(500));
        var paymentId = "payment-123";

        // Create a mock user
        var user = new User(new TelegramUserId(555555555), "testuser");
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _purchaseRepositoryMock
            .Setup(r => r.GetByTelegramPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Purchase?)null);

        _purchaseRepositoryMock
            .Setup(r => r.GetByIdAsync(purchase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase);

        var request = new PaymentVerificationRequest
        {
            TelegramPaymentId = paymentId,
            PurchaseId = purchase.Id,
            TelegramUserId = 555555555,
            Amount = 500,
            Currency = "USD" // Invalid currency
        };

        // Act
        var result = await _service.VerifyPaymentAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Telegram Stars");
    }

    [Fact]
    public async Task IsPaymentAlreadyProcessedAsync_WithExistingPayment_ReturnsTrue()
    {
        // Arrange
        var paymentId = "payment-123";
        var purchase = new PurchasePhoto(Guid.NewGuid(), Guid.NewGuid(), new TelegramStars(500));
        purchase.MarkPaymentCompleted(paymentId);

        _purchaseRepositoryMock
            .Setup(r => r.GetByTelegramPaymentIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(purchase);

        // Act
        var result = await _service.IsPaymentAlreadyProcessedAsync(paymentId);

        // Assert
        result.Should().BeTrue();
    }
}

