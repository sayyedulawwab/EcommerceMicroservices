using SharedKernel.Domain;
using ShoppingCart = Cart.Domain.Carts.Cart;

namespace Cart.Application.UnitTests.Carts;

internal static class CartTestData
{
    public const long UserId = 7;
    public const long ProductA = 1;
    public const long ProductB = 2;

    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UtcNow = new(2026, 1, 2, 12, 30, 0, DateTimeKind.Utc);

    public static Money Usd(decimal amount) => new(amount, Currency.Usd);

    /// <summary>Keyboard 2 x 10 + Mouse 3 x 5 = 35 USD.</summary>
    public static ShoppingCart CreateCartWithTwoItems()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);
        cart.AddItem(ProductA, "Keyboard", Usd(10m), 2, CreatedOnUtc);
        cart.AddItem(ProductB, "Mouse", Usd(5m), 3, CreatedOnUtc);

        return cart;
    }
}
