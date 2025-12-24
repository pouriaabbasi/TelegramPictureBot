using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Services;

/// <summary>
/// Service for discovering and browsing content creator models
/// </summary>
public class ModelDiscoveryService : IModelDiscoveryService
{
    private readonly IModelRepository _modelRepository;
    private readonly IPhotoRepository _photoRepository;

    public ModelDiscoveryService(
        IModelRepository modelRepository,
        IPhotoRepository photoRepository)
    {
        _modelRepository = modelRepository;
        _photoRepository = photoRepository;
    }

    public async Task<IEnumerable<Model>> BrowseModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _modelRepository.GetApprovedModelsAsync(0, 100, cancellationToken); // Get first 100 models
    }

    public async Task<Model?> GetModelProfileAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        
        // Only return approved models for public viewing
        if (model == null || model.Status != ModelStatus.Approved)
        {
            return null;
        }

        return model;
    }

    public async Task<IEnumerable<Photo>> GetModelPhotosAsync(Guid modelId, PhotoType? type = null, CancellationToken cancellationToken = default)
    {
        // Verify model exists and is approved
        var model = await GetModelProfileAsync(modelId, cancellationToken);
        if (model == null)
        {
            return Enumerable.Empty<Photo>();
        }

        // Get all photos and filter locally
        var allPhotos = await _photoRepository.GetAllAsync(cancellationToken);
        
        // Filter by modelId
        var result = allPhotos.Where(p => p.ModelId == modelId);
        
        // If type is specified, filter by type
        if (type.HasValue)
        {
            result = result.Where(p => p.Type == type.Value);
        }
        else
        {
            // If no type specified, only return items that are for sale (excludes Demo by default)
            result = result.Where(p => p.IsForSale);
        }

        return result;
    }

    public async Task<IEnumerable<Photo>> GetModelPremiumPhotosAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await GetModelPhotosAsync(modelId, PhotoType.Premium, cancellationToken);
    }

    public async Task<IEnumerable<Photo>> GetModelDemoPhotosAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await GetModelPhotosAsync(modelId, PhotoType.Demo, cancellationToken);
    }

    public async Task<IEnumerable<Model>> SearchModelsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await BrowseModelsAsync(cancellationToken);
        }

        return await _modelRepository.SearchByNameAsync(searchTerm, cancellationToken);
    }

    public async Task<ModelStatistics> GetModelStatisticsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        // Get ALL photos for statistics (both premium and demo)
        var allPhotos = await _photoRepository.GetAllAsync(cancellationToken);
        var modelPhotos = allPhotos.Where(p => p.ModelId == modelId).ToList();

        return new ModelStatistics
        {
            TotalSubscribers = model.TotalSubscribers,
            TotalContentItems = model.TotalContentItems,
            PremiumPhotos = modelPhotos.Count(p => p.Type == PhotoType.Premium),
            DemoPhotos = modelPhotos.Count(p => p.Type == PhotoType.Demo),
            HasSubscriptionAvailable = model.CanAcceptSubscriptions(),
            SubscriptionPrice = model.SubscriptionPrice?.Amount,
            SubscriptionDurationDays = model.SubscriptionDurationDays
        };
    }
}

