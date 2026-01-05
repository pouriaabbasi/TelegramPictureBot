using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

public class SentMessageInfo
{
    public int MessageId { get; set; }
    public long ChatId { get; set; }
}

/// <summary>
/// Service for interacting with Telegram Bot API
/// </summary>
public interface ITelegramBotService
{
    /// <summary>
    /// Sends a message to a user
    /// </summary>
    Task<bool> SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message and returns the sent message info
    /// </summary>
    Task<SentMessageInfo> SendMessageWithReturnAsync(long chatId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message with inline keyboard buttons
    /// </summary>
    Task<bool> SendMessageWithButtonsAsync(long chatId, string message, object keyboard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits an existing message text
    /// </summary>
    Task<bool> EditMessageAsync(long chatId, int messageId, string newText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits an existing message text
    /// </summary>
    Task<bool> EditMessageTextAsync(long chatId, int messageId, string newText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits an existing message text and removes inline keyboard
    /// </summary>
    Task<bool> EditMessageTextAndRemoveKeyboardAsync(long chatId, int messageId, string newText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a message
    /// </summary>
    Task<bool> DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a contact card to a user
    /// </summary>
    Task<bool> SendContactAsync(long chatId, string phoneNumber, string firstName, string? lastName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a photo to a user
    /// </summary>
    Task<bool> SendPhotoAsync(long chatId, string photoPath, string? caption = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a video to a user
    /// </summary>
    Task<bool> SendVideoAsync(long chatId, string videoPath, string? caption = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an invoice for Telegram Stars payment
    /// </summary>
    Task<string?> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers a pre-checkout query (required before payment completion)
    /// </summary>
    Task<bool> AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies payment with Telegram
    /// </summary>
    Task<bool> VerifyPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
}

