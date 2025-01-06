using System.Linq.Expressions;

namespace Catalog.Domain.Products;
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product>, int TotalRecords)> FindAsync(
        Expression<Func<Product, bool>>? predicate = null,
        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    void Add(Product product);
    void Update(Product product);
    void Remove(Product product);
}