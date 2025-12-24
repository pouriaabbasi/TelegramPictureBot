using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Interfaces;

public interface IPhotoRepository : IRepository<Photo>
{
    Task<IEnumerable<Photo>> GetAvailablePhotosAsync(CancellationToken cancellationToken = default);
}

