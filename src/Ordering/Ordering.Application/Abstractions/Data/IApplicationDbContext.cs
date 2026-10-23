using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Orders;

namespace Ordering.Application.Abstractions.Data;

// Handlers load and save aggregates through the DbContext directly.
public interface IApplicationDbContext
{
    DbSet<Order> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
