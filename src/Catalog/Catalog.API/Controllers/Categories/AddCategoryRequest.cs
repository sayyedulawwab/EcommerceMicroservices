namespace Catalog.API.Controllers.Categories;

public record AddCategoryRequest
{
    public string Name { get; init; }
    public string Description { get; init; }
    public long? ParentCategoryId { get; init; }
}