using Catalog.Domain.Abstractions;

namespace Catalog.Domain.Products;
public static class ProductErrors
{
    public static Error NotFound(long id) => new (
        "Product.NotFound",
        $"The Product with the Id = '{id}' was not found");

    public static Error NotFound() => new(
       "Product.NotFound",
       $"The Products with this filter was not found");
}
