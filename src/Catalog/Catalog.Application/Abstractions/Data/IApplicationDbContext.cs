using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Abstractions.Data;

// Handlers load and save aggregates through the DbContext directly.
public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<StockReservation> StockReservations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
