namespace Cart.Application.Carts.Queries.GetCartByUserId;

public sealed class CartResponse
{
    public Guid Id { get; init; }
    public long UserId { get; init; }
    public List<CartItemResponse> CartItems { get; init; } = new List<CartItemResponse>();
    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

}
