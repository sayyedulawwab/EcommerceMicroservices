namespace Cart.Domain.Carts;

public record CartId(Guid Value)
{
    public static CartId New() => new(Guid.CreateVersion7());
}
