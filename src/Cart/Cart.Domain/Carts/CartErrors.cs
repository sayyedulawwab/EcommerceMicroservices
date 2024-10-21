using Cart.Domain.Abstractions;

namespace Cart.Domain.Carts;
public static class CartErrors
{

    public static Error NotFound() => new(
       "Cart.NotFound",
       $"The Cart with this filter was not found");
}
