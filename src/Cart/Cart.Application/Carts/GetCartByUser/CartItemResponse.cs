namespace Cart.Application.Carts.GetCartByUser;

public sealed class CartItemResponse
{
    public long ProductId { get; init; }
    public string ProductName { get; init; }
    public decimal ProductPriceAmount { get; init; }
    public string ProductPriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

