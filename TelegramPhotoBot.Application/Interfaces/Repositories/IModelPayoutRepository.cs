using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces.Repositories;

public interface IModelPayoutRepository : IRepository<ModelPayout>
{
    /// <summary>
    /// Get all payouts for a specific model
    /// </summary>
    Task<IEnumerable<ModelPayout>> GetModelPayoutsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get completed payouts for a model
    /// </summary>
    Task<IEnumerable<ModelPayout>> GetCompletedPayoutsAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get pending payouts for admin review
    /// </summary>
    Task<IEnumerable<ModelPayout>> GetPendingPayoutsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get total paid amount for a model (in Stars)
    /// </summary>
    Task<long> GetTotalPaidAmountAsync(Guid modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get latest payout for a model
    /// </summary>
    Task<ModelPayout?> GetLatestPayoutAsync(Guid modelId, CancellationToken cancellationToken = default);
}
