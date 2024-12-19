namespace Catalog.API.Controllers.Categories.AddCategory;

public record AddCategoryRequest(string name, string description, long parentCategoryId);