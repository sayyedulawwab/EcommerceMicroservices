namespace Ordering.Domain.Orders;

public enum OrderStatus
{
    Placed = 1,
    StockConfirmed = 2,
    Paid = 3,
    Delivered = 4,
    Cancelled = 5,
}