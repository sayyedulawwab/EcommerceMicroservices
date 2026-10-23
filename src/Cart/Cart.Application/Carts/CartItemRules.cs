using Cart.Domain.Carts;
using FluentValidation;
using SharedKernel.Domain;

namespace Cart.Application.Carts;

internal static class CartItemRules
{
    public const int MaxQuantity = CartItem.MaxQuantity;

    public static void ProductId<T>(IRuleBuilder<T, Guid> rule) => rule.NotEmpty();

    public static void ProductName<T>(IRuleBuilder<T, string> rule) => rule.NotEmpty().MaximumLength(200);

    public static void PriceAmount<T>(IRuleBuilder<T, decimal> rule) => rule.GreaterThan(0);

    public static void PriceCurrency<T>(IRuleBuilder<T, string> rule) =>
        rule.Must(Currency.IsSupported).WithMessage("The currency is not supported");

    public static void Quantity<T>(IRuleBuilder<T, int> rule) => rule.InclusiveBetween(1, MaxQuantity);
}
