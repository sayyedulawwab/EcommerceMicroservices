using System.Text.Json.Serialization;

namespace Ordering.API.Controllers.Orders;

public record OrderItemRequest
{
    [JsonRequired] public long ProductId { get; init; }
    [JsonRequired] public int Quantity { get; init; }
}

public record PlaceOrderRequest
{
    public List<OrderItemRequest> OrderItems { get; init; } = [];
}
