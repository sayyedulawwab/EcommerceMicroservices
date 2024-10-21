using Catalog.Domain.Abstractions;

namespace Catalog.Domain.Products;
public static class ProductErrors
{

    public static Error NotFound() => new(
       "Product.NotFound",
       $"The Products with this filter was not found");
}
