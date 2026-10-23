namespace Ordering.Domain.Orders;

public record OrderItemId(Guid Value)
{
    public static OrderItemId New() => new(Guid.CreateVersion7());
}
