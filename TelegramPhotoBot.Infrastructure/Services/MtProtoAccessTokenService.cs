using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Services;

public class MtProtoAccessTokenService : IMtProtoAccessTokenService
{
    private readonly ApplicationDbContext _dbContext;

    public MtProtoAccessTokenService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateTokenAsync(Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var token = new MtProtoAccessToken
        {
            AdminUserId = adminUserId
        };

        _dbContext.MtProtoAccessTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        Console.WriteLine($"🔑 Generated MTProto access token for admin {adminUserId}: {token.Token}");
        return token.Token.ToString();
    }

    public async Task<bool> ValidateAndConsumeTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        var accessToken = await _dbContext.MtProtoAccessTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (accessToken == null)
        {
            Console.WriteLine($"❌ Token {token} not found");
            return false;
        }

        if (!accessToken.IsValid())
        {
            Console.WriteLine($"❌ Token {token} is invalid (used={accessToken.IsUsed}, expired={DateTime.UtcNow >= accessToken.ExpiresAt})");
            return false;
        }

        // Mark as used
        accessToken.IsUsed = true;
        accessToken.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        Console.WriteLine($"✅ Token {token} validated and consumed");
        return true;
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _dbContext.MtProtoAccessTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Any())
        {
            _dbContext.MtProtoAccessTokens.RemoveRange(expiredTokens);
            await _dbContext.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"🗑️ Cleaned up {expiredTokens.Count} expired MTProto access tokens");
        }
    }
}

