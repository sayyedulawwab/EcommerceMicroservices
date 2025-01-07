using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Catalog.Infrastructure.Repositories;
internal sealed class ProductRepository(ApplicationDbContext dbContext) : Repository<Product>(dbContext), IProductRepository
{
    public async Task<(IReadOnlyList<Product>, int TotalRecords)> FindAsync(Expression<Func<Product, bool>>? predicate = null, Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = DbContext.Set<Product>();

        // Apply the filter if provided
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // Calculate total count without pagination
        int totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting if provided
        if (orderBy != null)
        {
            query = orderBy(query);
        }
        else
        {
            query = query.OrderBy(p => p.CreatedOnUtc); // Default sorting
        }

        // Apply pagination
        List<Product> pagedProducts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (pagedProducts, totalCount);
    }

}