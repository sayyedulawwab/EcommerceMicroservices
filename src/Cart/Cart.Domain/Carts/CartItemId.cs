namespace Cart.Domain.Carts;

public record CartItemId(Guid Value)
{
    public static CartItemId New() => new(Guid.CreateVersion7());
}
