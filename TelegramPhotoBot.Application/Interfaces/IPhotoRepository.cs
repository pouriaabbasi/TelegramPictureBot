using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Application.DTOs;

namespace TelegramPhotoBot.Application.Interfaces;

public interface IPhotoRepository : IRepository<Photo>
{
    Task<IEnumerable<Photo>> GetAvailablePhotosAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentStatisticsDto>> GetContentStatisticsAsync(Guid modelId, CancellationToken cancellationToken = default);
}

