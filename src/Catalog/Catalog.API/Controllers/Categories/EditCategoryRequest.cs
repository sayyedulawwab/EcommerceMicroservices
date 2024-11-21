namespace Catalog.API.Controllers.Categories;

public record EditCategoryRequest(string name, string description, long parentCategoryId);