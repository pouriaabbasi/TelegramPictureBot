using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Services;

/// <summary>
/// Service for handling authorization and role-based access control
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly IModelRepository _modelRepository;
    private readonly IPhotoRepository _photoRepository;

    public AuthorizationService(
        IUserRepository userRepository,
        IModelRepository modelRepository,
        IPhotoRepository photoRepository)
    {
        _userRepository = userRepository;
        _modelRepository = modelRepository;
        _photoRepository = photoRepository;
    }

    public async Task<bool> HasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.Role == role;
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await HasRoleAsync(userId, UserRole.Admin, cancellationToken);
    }

    public async Task<bool> IsModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.Role == UserRole.Model && user.ModelId.HasValue;
    }

    public async Task<bool> IsModelOwnerAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        return model != null && model.UserId == userId;
    }

    public async Task<bool> IsPhotoOwnerAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
        return photo != null && photo.SellerId == userId;
    }

    public async Task EnsureHasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        if (!await HasRoleAsync(userId, role, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User does not have the required role: {role}");
        }
    }

    public async Task EnsureIsAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await IsAdminAsync(userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("User is not an admin");
        }
    }

    public async Task EnsureIsModelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await IsModelAsync(userId, cancellationToken))
        {
            throw new UnauthorizedAccessException("User is not a model");
        }
    }

    public async Task EnsureIsModelOwnerAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default)
    {
        if (!await IsModelOwnerAsync(userId, modelId, cancellationToken))
        {
            throw new UnauthorizedAccessException("User does not own this model");
        }
    }
}

