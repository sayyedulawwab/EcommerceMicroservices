using SharedKernel.Domain;

namespace Cart.Domain.Carts;

public static class CartErrors
{
    public static Error NotFound() => Error.NotFound(
       "Cart.NotFound",
       $"The Cart with this filter was not found");

    public static Error ItemNotFound(long productId) => Error.NotFound(
        "Cart.ItemNotFound",
        $"Product {productId} is not in the cart");

    public static Error InvalidQuantity(long productId) => Error.BadRequest(
        "Cart.InvalidQuantity",
        $"The quantity for product {productId} is not valid");

    public static readonly Error MixedCurrencies = Error.BadRequest(
        "Cart.MixedCurrencies",
        "All items in a cart must be priced in the same currency");
}
