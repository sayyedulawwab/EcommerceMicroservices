using Catalog.Domain.Categories;

namespace Catalog.Infrastructure.Repositories;
internal sealed class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}