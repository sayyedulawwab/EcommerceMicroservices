using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

internal sealed class UserTokenRepository(ApplicationDbContext dbContext) : Repository<UserToken>(dbContext), IUserTokenRepository
{
    public async Task<UserToken?> GetAsync(long userId, UserTokenPurpose purpose, string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<UserToken>()
            .FirstOrDefaultAsync(
                t => t.UserId == userId && t.Purpose == purpose && t.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task ConsumeAllAsync(long userId, UserTokenPurpose purpose, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<UserToken>()
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.ConsumedOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ConsumedOnUtc, utcNow), cancellationToken);
    }

    public async Task<int> DeleteStaleAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<UserToken>()
            .Where(t => t.ExpiresOnUtc < olderThanUtc || t.ConsumedOnUtc < olderThanUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
