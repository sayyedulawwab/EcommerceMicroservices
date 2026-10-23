namespace Catalog.Application.Categories;

public sealed class CategoryResponse
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public long? ParentCategoryId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}
