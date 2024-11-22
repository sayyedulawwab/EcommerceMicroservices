using SharedKernel.Domain;

namespace Cart.Domain.Carts;
public static class CartErrors
{
    public static Error NotFound() => Error.NotFound(
       "Cart.NotFound",
       $"The Cart with this filter was not found");
}