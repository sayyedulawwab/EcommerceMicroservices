using SharedKernel.Domain;

namespace Catalog.Domain.Products;
public static class ProductErrors
{
    public static Error NotFound() => Error.NotFound(
       "Product.NotFound",
       $"The Products with this filter was not found");

    public static Error SoldOut(long id) => Error.NotFound(
       "Product.SoldOut",
       $"Empty stock, product item {id} is sold out");

}
