using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UserDto> GetOrCreateUserAsync(TelegramUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var telegramUserId = new TelegramUserId(userInfo.Id);
        var existingUser = await _userRepository.GetByTelegramUserIdAsync(userInfo.Id, cancellationToken);

        if (existingUser != null)
        {
            // Update user information if changed
            existingUser.UpdateProfile(userInfo.FirstName, userInfo.LastName, userInfo.Username);
            await _userRepository.UpdateAsync(existingUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToDto(existingUser);
        }

        // Create new user
        var newUser = new User(
            telegramUserId,
            userInfo.Username,
            userInfo.FirstName,
            userInfo.LastName,
            userInfo.LanguageCode,
            userInfo.IsBot);

        await _userRepository.AddAsync(newUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(newUser);
    }

    public async Task<UserDto?> GetUserByTelegramIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        return user != null ? MapToDto(user) : null;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            TelegramUserId = user.TelegramUserId.Value,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive
        };
    }
}

