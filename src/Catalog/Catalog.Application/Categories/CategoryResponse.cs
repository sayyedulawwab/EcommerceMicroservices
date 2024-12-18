namespace Catalog.Application.Categories;
public sealed class CategoryResponse
{
    public long Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public long ParentCategoryId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}