using Ordering.Domain.Abstractions;

namespace Ordering.Domain.Orders;
public static class OrderErrors
{

    public static Error NotFound() => new(
       "Order.NotFound",
       $"The Orders with this filter was not found");
}
