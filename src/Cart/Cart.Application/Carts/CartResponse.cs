namespace Cart.Application.Carts;

public sealed class CartResponse
{
    public Guid Id { get; init; }
    public long UserId { get; init; }
    public List<CartItemResponse> CartItems { get; init; } = [];
    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

    public static CartResponse FromCart(Domain.Carts.Cart cart)
    {
        return new CartResponse
        {
            Id = cart.Id,
            UserId = cart.UserId,
            CartItems = cart.CartItems.Select(ci => new CartItemResponse
            {
                ProductId = ci.ProductId,
                ProductName = ci.ProductName,
                ProductPriceAmount = ci.Price.Amount,
                ProductPriceCurrency = ci.Price.Currency.Code,
                Quantity = ci.Quantity,
                CreatedOnUtc = ci.CreatedOnUtc
            }).ToList(),
            TotalPriceAmount = cart.TotalPrice.Amount,
            TotalPriceCurrency = cart.TotalPrice.Currency.Code,
            CreatedOnUtc = cart.CreatedOnUtc,
            UpdatedOnUtc = cart.UpdatedOnUtc
        };
    }

    /// <summary>What a user without a stored cart gets back: an empty cart, not a 404.</summary>
    public static CartResponse Empty(long userId)
    {
        return new CartResponse
        {
            Id = Guid.Empty,
            UserId = userId,
            TotalPriceAmount = 0,
            TotalPriceCurrency = string.Empty
        };
    }
}
