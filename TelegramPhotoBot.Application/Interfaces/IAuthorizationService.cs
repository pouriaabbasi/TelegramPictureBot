using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for handling authorization and role-based access control
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if a user has a specific role
    /// </summary>
    Task<bool> HasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user is an admin
    /// </summary>
    Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user is a model (content creator)
    /// </summary>
    Task<bool> IsModelAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user owns a specific model
    /// </summary>
    Task<bool> IsModelOwnerAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user owns a specific photo
    /// </summary>
    Task<bool> IsPhotoOwnerAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures a user has a specific role, throws exception if not
    /// </summary>
    Task EnsureHasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures a user is an admin, throws exception if not
    /// </summary>
    Task EnsureIsAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures a user is a model, throws exception if not
    /// </summary>
    Task EnsureIsModelAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures a user owns a specific model, throws exception if not
    /// </summary>
    Task EnsureIsModelOwnerAsync(Guid userId, Guid modelId, CancellationToken cancellationToken = default);
}

