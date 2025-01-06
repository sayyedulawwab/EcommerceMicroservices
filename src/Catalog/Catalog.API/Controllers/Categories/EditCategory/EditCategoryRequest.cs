using System.Text.Json.Serialization;

namespace Catalog.API.Controllers.Categories.EditCategory;

public record EditCategoryRequest
{
    public string Name { get; init; }
    public string Description { get; init; }
    [JsonRequired] public long ParentCategoryId { get; init; }
}