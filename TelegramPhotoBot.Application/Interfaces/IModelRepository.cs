using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Repository interface for Model aggregate
/// </summary>
public interface IModelRepository : IRepository<Model>
{
    /// <summary>
    /// Get model by user ID
    /// </summary>
    Task<Model?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all models with a specific status
    /// </summary>
    Task<IEnumerable<Model>> GetByStatusAsync(ModelStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get approved models with pagination
    /// </summary>
    Task<IEnumerable<Model>> GetApprovedModelsAsync(int skip, int take, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get models pending approval
    /// </summary>
    Task<IEnumerable<Model>> GetPendingModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search models by display name
    /// </summary>
    Task<IEnumerable<Model>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user is already a model
    /// </summary>
    Task<bool> IsUserAModelAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get model with related data (photos, subscriptions)
    /// </summary>
    Task<Model?> GetWithRelatedDataAsync(Guid modelId, CancellationToken cancellationToken = default);
}

