namespace Ordering.Application.Orders;

public sealed class OrderItemResponse
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public decimal ProductPriceAmount { get; init; }
    public required string ProductPriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
