namespace Cart.Application.Carts.GetCartByUser;

public sealed class CartResponse
{
    public Guid Id { get; init; }
    public long UserId { get; init; }
    public List<CartItemResponse> CartItems { get; init; } = [];
    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
