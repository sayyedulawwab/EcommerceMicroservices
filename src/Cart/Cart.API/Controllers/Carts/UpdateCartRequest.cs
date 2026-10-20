using System.Text.Json.Serialization;

namespace Cart.API.Controllers.Carts;

public record UpdateCartRequest
{
    public List<CartItemRequest> CartItems { get; init; } = [];
}

public record CartItemRequest
{
    [JsonRequired] public long ProductId { get; init; }
    public string ProductName { get; init; }
    [JsonRequired] public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; }
    [JsonRequired] public int Quantity { get; init; }
}

public record UpdateCartItemQuantityRequest
{
    [JsonRequired] public int Quantity { get; init; }
}
