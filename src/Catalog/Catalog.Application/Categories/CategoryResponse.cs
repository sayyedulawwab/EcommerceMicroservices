namespace Catalog.Application.Categories;
public sealed class CategoryResponse
{
    public long Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public long ParentCategoryId { get; init; }
    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }
}