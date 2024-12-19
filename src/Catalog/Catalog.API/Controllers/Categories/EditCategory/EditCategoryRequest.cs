namespace Catalog.API.Controllers.Categories.EditCategory;

public record EditCategoryRequest(string name, string description, long parentCategoryId);