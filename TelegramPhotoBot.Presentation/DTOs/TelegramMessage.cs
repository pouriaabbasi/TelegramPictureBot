using Telegram.Bot.Types;

namespace TelegramPhotoBot.Presentation.DTOs;

/// <summary>
/// Telegram message DTO from Bot API
/// </summary>
public class TelegramMessage
{
    public long MessageId { get; init; }
    public TelegramUser From { get; init; } = null!;
    public long ChatId { get; init; }
    public string? Text { get; init; }
    public DateTime Date { get; init; }
    public PhotoSize[]? Photo { get; init; }
    public Video? Video { get; init; }
}

public class TelegramUser
{
    public long Id { get; init; }
    public bool IsBot { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Username { get; init; }
    public string? LanguageCode { get; init; }
}

