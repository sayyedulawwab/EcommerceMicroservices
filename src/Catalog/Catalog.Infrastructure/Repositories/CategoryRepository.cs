using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

internal sealed class CategoryRepository(ApplicationDbContext dbContext) : Repository<Category>(dbContext), ICategoryRepository
{
    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Category>().AnyAsync(category => category.Id == id, cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Category>().AnyAsync(category => category.ParentCategoryId == id, cancellationToken);
    }
}
