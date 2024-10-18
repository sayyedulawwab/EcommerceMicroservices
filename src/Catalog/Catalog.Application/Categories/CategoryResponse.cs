using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;
public sealed class CategoryResponse
{
    public long Id { get; init; }
    public string Name { get; init; }
    public string Code { get; init; }
}
