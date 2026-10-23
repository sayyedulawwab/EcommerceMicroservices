using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Orders;

namespace Ordering.Application.Abstractions.Data;

// Handlers load and save aggregates through the DbContext directly.
public interface IApplicationDbContext
{
    DbSet<Order> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Runs the operation in one database transaction, or inside the one already open (e.g. the consumer outbox's).
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
