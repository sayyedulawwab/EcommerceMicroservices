namespace Catalog.Domain.Categories;
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    void Add(Category category);
    void Update(Category category);
    void Remove(Category category);
}
