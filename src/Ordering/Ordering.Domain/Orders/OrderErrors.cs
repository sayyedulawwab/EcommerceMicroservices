using SharedKernel.Domain;

namespace Ordering.Domain.Orders;

public static class OrderErrors
{
    public static Error NotFound() => Error.NotFound(
       "Order.NotFound",
       $"The Orders with this filter was not found");

    public static readonly Error Empty = Error.BadRequest(
        "Order.Empty",
        "An order must contain at least one item");

    public static Error InvalidQuantity(long productId) => Error.BadRequest(
        "Order.InvalidQuantity",
        $"The quantity for product {productId} must be greater than zero");

    public static Error InvalidStatusTransition(OrderStatus from, OrderStatus to) => Error.Conflict(
        "Order.InvalidStatusTransition",
        $"An order can not move from {from} to {to}");

    public static Error CannotCancel(OrderStatus status) => Error.Conflict(
        "Order.CannotCancel",
        $"An order that is {status} can no longer be cancelled");

    public static Error MissingPrice(long productId) => Error.BadRequest(
        "Order.MissingPrice",
        $"No confirmed price was received for product {productId}");

    public static Error UnsupportedCurrency(string currency) => Error.BadRequest(
        "Order.UnsupportedCurrency",
        $"The currency '{currency}' is not supported");

    public static readonly Error MixedCurrencies = Error.BadRequest(
        "Order.MixedCurrencies",
        "All items of an order must be priced in the same currency");
}
