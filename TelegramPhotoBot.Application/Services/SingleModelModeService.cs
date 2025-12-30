using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Services;

/// <summary>
/// Service for managing single model mode settings
/// </summary>
public class SingleModelModeService : ISingleModelModeService
{
    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly IModelRepository _modelRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SingleModelModeService(
        IPlatformSettingsRepository platformSettingsRepository,
        IModelRepository modelRepository,
        IUnitOfWork unitOfWork)
    {
        _platformSettingsRepository = platformSettingsRepository ?? throw new ArgumentNullException(nameof(platformSettingsRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> IsSingleModelModeAsync(CancellationToken cancellationToken = default)
    {
        var value = await _platformSettingsRepository.GetValueAsync(
            PlatformSettings.Keys.SingleModelMode, 
            cancellationToken);
        
        return bool.TryParse(value, out var enabled) && enabled;
    }

    public async Task<Guid?> GetDefaultModelIdAsync(CancellationToken cancellationToken = default)
    {
        var value = await _platformSettingsRepository.GetValueAsync(
            PlatformSettings.Keys.DefaultModelId, 
            cancellationToken);
        
        return Guid.TryParse(value, out var modelId) ? modelId : null;
    }

    public async Task<Model?> GetDefaultModelAsync(CancellationToken cancellationToken = default)
    {
        var modelId = await GetDefaultModelIdAsync(cancellationToken);
        
        if (modelId == null)
            return null;
        
        return await _modelRepository.GetByIdAsync(modelId.Value, cancellationToken);
    }

    public async Task EnableSingleModelModeAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        // Verify model exists
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
            throw new InvalidOperationException($"Model with ID {modelId} not found");

        await _platformSettingsRepository.SetValueAsync(
            PlatformSettings.Keys.SingleModelMode,
            "true",
            "Enable single model mode - bot operates for one model only",
            false,
            cancellationToken);

        await _platformSettingsRepository.SetValueAsync(
            PlatformSettings.Keys.DefaultModelId,
            modelId.ToString(),
            $"Default model ID for single model mode: {model.DisplayName}",
            false,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableSingleModelModeAsync(CancellationToken cancellationToken = default)
    {
        await _platformSettingsRepository.SetValueAsync(
            PlatformSettings.Keys.SingleModelMode,
            "false",
            "Disable single model mode - browse all models",
            false,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

