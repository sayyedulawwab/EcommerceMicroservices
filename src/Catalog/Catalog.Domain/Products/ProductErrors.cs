using SharedKernel.Domain;

namespace Catalog.Domain.Products;

public static class ProductErrors
{
    public static Error NotFound() => Error.NotFound(
       "Product.NotFound",
       $"The Products with this filter was not found");

    public static Error InsufficientStock(Guid id) => Error.Conflict(
       "Product.InsufficientStock",
       $"Product {id} does not have enough stock");

    public static Error InvalidQuantity(Guid id) => Error.BadRequest(
       "Product.InvalidQuantity",
       $"The requested quantity for product {id} must be greater than zero");

    public static Error InvalidPrice() => Error.BadRequest(
       "Product.InvalidPrice",
       "The product price must be greater than zero");

    public static Error InvalidStockQuantity() => Error.BadRequest(
       "Product.InvalidStockQuantity",
       "The product stock quantity cannot be negative");

    public static Error CategoryNotFound(Guid categoryId) => Error.BadRequest(
       "Product.CategoryNotFound",
       $"The category with the Id = '{categoryId}' does not exist");
}
