using SharedKernel.Domain;

namespace Ordering.Domain.Orders;
public static class OrderErrors
{
    public static Error NotFound() => Error.NotFound(
       "Order.NotFound",
       $"The Orders with this filter was not found");
}