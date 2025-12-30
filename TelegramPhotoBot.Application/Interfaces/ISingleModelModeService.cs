using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Services;

/// <summary>
/// Interface for managing single model mode settings
/// </summary>
public interface ISingleModelModeService
{
    /// <summary>
    /// Check if single model mode is enabled
    /// </summary>
    Task<bool> IsSingleModelModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the default model ID for single model mode
    /// </summary>
    Task<Guid?> GetDefaultModelIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the default model entity for single model mode
    /// </summary>
    Task<Model?> GetDefaultModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable single model mode for a specific model
    /// </summary>
    Task EnableSingleModelModeAsync(Guid modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disable single model mode (return to browse all models)
    /// </summary>
    Task DisableSingleModelModeAsync(CancellationToken cancellationToken = default);
}

