using Catalog.Domain.Categories;

namespace Catalog.Infrastructure.Repositories;
internal sealed class CategoryRepository(ApplicationDbContext dbContext) : Repository<Category>(dbContext), ICategoryRepository
{
}