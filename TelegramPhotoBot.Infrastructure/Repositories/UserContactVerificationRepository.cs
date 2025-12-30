using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Data;

namespace TelegramPhotoBot.Infrastructure.Repositories;

public class UserContactVerificationRepository : IUserContactVerificationRepository
{
    private readonly ApplicationDbContext _context;

    public UserContactVerificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserContactVerification?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserContactVerifications
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<UserContactVerification> CreateAsync(UserContactVerification verification, CancellationToken cancellationToken = default)
    {
        await _context.UserContactVerifications.AddAsync(verification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return verification;
    }

    public async Task UpdateAsync(UserContactVerification verification, CancellationToken cancellationToken = default)
    {
        _context.UserContactVerifications.Update(verification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UserContactVerification>> GetPendingManualAddsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserContactVerifications
            .Include(x => x.User)
            .Where(x => !x.IsAutoAddedToSenderContacts && !x.IsAdminNotified)
            .ToListAsync(cancellationToken);
    }
}

