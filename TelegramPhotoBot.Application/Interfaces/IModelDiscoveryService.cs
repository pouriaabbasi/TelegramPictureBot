using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Interfaces;

/// <summary>
/// Service for discovering and browsing content creator models
/// </summary>
public interface IModelDiscoveryService
{
    /// <summary>
    /// Gets all approved models for browsing
    /// </summary>
    Task<IEnumerable<Model>> BrowseModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific model's profile with demo content
    /// </summary>
    Task<Model?> GetModelProfileAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all photos for a specific model
    /// </summary>
    Task<IEnumerable<Photo>> GetModelPhotosAsync(Guid modelId, PhotoType? type = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets premium photos for a specific model
    /// </summary>
    Task<IEnumerable<Photo>> GetModelPremiumPhotosAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets demo photos for a specific model
    /// </summary>
    Task<IEnumerable<Photo>> GetModelDemoPhotosAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches models by display name
    /// </summary>
    Task<IEnumerable<Model>> SearchModelsAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets model statistics (subscriber count, content count, etc.)
    /// </summary>
    Task<ModelStatistics> GetModelStatisticsAsync(Guid modelId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for a model
/// </summary>
public class ModelStatistics
{
    public int TotalSubscribers { get; set; }
    public int TotalContentItems { get; set; }
    public int PremiumPhotos { get; set; }
    public int DemoPhotos { get; set; }
    public bool HasSubscriptionAvailable { get; set; }
    public long? SubscriptionPrice { get; set; }
    public int? SubscriptionDurationDays { get; set; }
}

