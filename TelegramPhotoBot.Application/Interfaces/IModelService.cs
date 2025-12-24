using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;
using FileInfo = TelegramPhotoBot.Domain.ValueObjects.FileInfo;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for managing content creator models
/// </summary>
public interface IModelService
{
    /// <summary>
    /// Registers a new model (requires admin approval)
    /// </summary>
    Task<Model> RegisterModelAsync(Guid userId, string displayName, string? bio, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a model by ID
    /// </summary>
    Task<Model?> GetModelByIdAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a model by user ID
    /// </summary>
    Task<Model?> GetModelByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all approved models
    /// </summary>
    Task<IEnumerable<Model>> GetApprovedModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pending approval models (admin only)
    /// </summary>
    Task<IEnumerable<Model>> GetPendingApprovalModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Approves a model (admin only)
    /// </summary>
    Task<Model> ApproveModelAsync(Guid modelId, Guid approvedByAdminId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rejects a model (admin only)
    /// </summary>
    Task<Model> RejectModelAsync(Guid modelId, Guid rejectedByAdminId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates model profile
    /// </summary>
    Task<Model> UpdateProfileAsync(Guid modelId, string displayName, string? bio, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets or updates the demo image for a model
    /// </summary>
    Task<Model> SetDemoImageAsync(Guid modelId, FileInfo demoImage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes the demo image from a model
    /// </summary>
    Task<Model> RemoveDemoImageAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets subscription pricing for a model
    /// </summary>
    Task<Model> SetSubscriptionPricingAsync(Guid modelId, TelegramStars price, int durationDays, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Suspends a model (admin only)
    /// </summary>
    Task<Model> SuspendModelAsync(Guid modelId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reactivates a suspended model (admin only)
    /// </summary>
    Task<Model> ReactivateModelAsync(Guid modelId, CancellationToken cancellationToken = default);
}

