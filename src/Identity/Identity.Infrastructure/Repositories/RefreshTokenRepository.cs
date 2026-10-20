using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository(ApplicationDbContext dbContext) : Repository<RefreshToken>(dbContext), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<RefreshToken>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<RefreshToken>()
            .Where(r => r.FamilyId == familyId && r.RevokedOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedOnUtc, utcNow), cancellationToken);
    }

    public async Task RevokeAllForUserAsync(long userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<RefreshToken>()
            .Where(r => r.UserId == userId && r.RevokedOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedOnUtc, utcNow), cancellationToken);
    }

    public async Task<int> DeleteStaleAsync(DateTime expiredBeforeUtc, DateTime revokedBeforeUtc, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<RefreshToken>()
            .Where(r => r.ExpiresOnUtc < expiredBeforeUtc || r.RevokedOnUtc < revokedBeforeUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
