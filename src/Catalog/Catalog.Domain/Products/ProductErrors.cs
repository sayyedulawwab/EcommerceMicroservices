using Catalog.Domain.Abstractions;
using System.Xml.Linq;

namespace Catalog.Domain.Products;
public static class ProductErrors
{

    public static Error NotFound() => new(
       "Product.NotFound",
       $"The Products with this filter was not found");

    public static Error SoldOut(long id) => new(
       "Product.SoldOut",
       $"Empty stock, product item {id} is sold out");

}
