namespace Catalog.Domain.Products;

public record ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.CreateVersion7());
}
