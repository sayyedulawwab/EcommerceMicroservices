namespace Ordering.Application.Orders.GetAllOrders;

public sealed class OrderResponse
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public List<OrderItemResponse> OrderItems { get; init; } = new List<OrderItemResponse>();
    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; }
    public string Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ShippedOnUtc { get; init; }
    public DateTime? DeliveredOnUtc { get; init; }
    public DateTime? CancelledOnUtc { get; init; }


}
