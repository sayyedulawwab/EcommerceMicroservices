using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Abstractions.Data;

// Handlers load and save aggregates through the DbContext directly.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserToken> UserTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> in one transaction, for work that can't commit in a single save
    /// (an <c>ExecuteUpdate</c> next to tracked changes). Joins a transaction that is already open.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
