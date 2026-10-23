namespace Catalog.Domain.Categories;

public record CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.CreateVersion7());
}
