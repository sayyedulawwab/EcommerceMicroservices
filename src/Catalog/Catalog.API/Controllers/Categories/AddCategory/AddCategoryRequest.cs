using System.Text.Json.Serialization;

namespace Catalog.API.Controllers.Categories.AddCategory;

public record AddCategoryRequest
{
    public string Name { get; init; }
    public string Description { get; init; }
    [JsonRequired] public long ParentCategoryId { get; init; }
}