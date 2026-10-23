namespace Catalog.API.Controllers.Categories;

public record AddCategoryRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public Guid? ParentCategoryId { get; init; }
}