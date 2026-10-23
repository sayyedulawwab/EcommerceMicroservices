namespace Ordering.Domain.Orders;

public record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
}
