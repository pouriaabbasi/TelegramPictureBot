using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface IModelTermsAcceptanceRepository : IRepository<ModelTermsAcceptance>
{
    /// <summary>
    /// Get the latest terms acceptance for a model
    /// </summary>
    Task<ModelTermsAcceptance?> GetLatestAcceptanceAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all terms acceptances for a model (history)
    /// </summary>
    Task<IEnumerable<ModelTermsAcceptance>> GetModelAcceptanceHistoryAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a model has accepted the latest terms version
    /// </summary>
    Task<bool> HasAcceptedLatestTermsAsync(Guid modelId, string latestVersion, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark all previous acceptances as old when a new version is accepted
    /// </summary>
    Task MarkPreviousAsOldVersionAsync(Guid modelId, CancellationToken cancellationToken = default);
}
