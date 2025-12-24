namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Telegram user information from Bot API
/// </summary>
public class TelegramUserInfo
{
    public long Id { get; init; }
    public bool IsBot { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Username { get; init; }
    public string? LanguageCode { get; init; }
}

