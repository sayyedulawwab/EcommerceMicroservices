namespace Ordering.Application.Orders.GetAllOrders;

public sealed class OrderItemResponse
{
    public long OrderId { get; init; }
    public long ProductId { get; init; }
    public string ProductName { get; init; }
    public decimal ProductPriceAmount { get; init; }
    public string ProductPriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

