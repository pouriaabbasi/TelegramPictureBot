using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Handles payment-related operations
/// Note: Star Reaction payment will be implemented when Telegram.Bot library v21.0.0+ is available
/// For now, only Telegram Invoice is supported
/// </summary>
public partial class TelegramUpdateHandler
{
    private IPendingStarPaymentRepository? _pendingStarPaymentRepository;

    // Payment functionality is currently handled by existing HandleBuyPhotoCommandAsync
    // Star Reaction payment infrastructure is ready but requires Telegram.Bot v21.0.0+
    // PendingStarPayment entity and repository are available for future implementation
}
