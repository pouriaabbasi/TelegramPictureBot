using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for managing users
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets or creates a user from Telegram user information
    /// </summary>
    Task<UserDto> GetOrCreateUserAsync(TelegramUserInfo userInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by Telegram user ID
    /// </summary>
    Task<UserDto?> GetUserByTelegramIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
}

