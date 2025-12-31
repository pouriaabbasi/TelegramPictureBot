using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces;

public interface IModelTermsService
{
    /// <summary>
    /// Get the current terms and conditions version
    /// </summary>
    string GetCurrentTermsVersion();
    
    /// <summary>
    /// Get the full terms and conditions text
    /// </summary>
    string GetTermsContent();
    
    /// <summary>
    /// Check if a model has accepted the terms
    /// </summary>
    Task<bool> HasAcceptedTermsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a model has accepted the latest terms version
    /// </summary>
    Task<bool> HasAcceptedLatestTermsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Record a model's acceptance of the terms
    /// </summary>
    Task<ModelTermsAcceptance> RecordAcceptanceAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the latest acceptance record for a model
    /// </summary>
    Task<ModelTermsAcceptance?> GetLatestAcceptanceAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get acceptance history for a model
    /// </summary>
    Task<IEnumerable<ModelTermsAcceptance>> GetAcceptanceHistoryAsync(Guid modelId, CancellationToken cancellationToken = default);
}
