namespace Catalog.API.Controllers.Categories;

public record AddCategoryRequest(string name, string description, long parentCategoryId);