namespace Catalog.API.Controllers.Categories;

public record EditCategoryRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public Guid? ParentCategoryId { get; init; }
}