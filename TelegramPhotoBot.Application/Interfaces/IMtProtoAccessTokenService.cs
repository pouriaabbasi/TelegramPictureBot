namespace TelegramPhotoBot.Application.Interfaces;

public interface IMtProtoAccessTokenService
{
    Task<string> GenerateTokenAsync(Guid adminUserId, CancellationToken cancellationToken = default);
    Task<bool> ValidateAndConsumeTokenAsync(Guid token, CancellationToken cancellationToken = default);
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}

