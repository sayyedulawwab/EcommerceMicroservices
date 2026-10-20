using FluentValidation;

namespace Cart.Application.Carts.ReplaceCart;

public class ReplaceCartCommandValidator : AbstractValidator<ReplaceCartCommand>
{
    private const int MaxItems = 100;

    public ReplaceCartCommandValidator()
    {
        RuleFor(c => c.UserId).GreaterThan(0);

        RuleFor(c => c.CartItems)
            .NotNull()
            .Must(items => items.Count <= MaxItems)
            .WithMessage($"A cart can contain at most {MaxItems} items");

        RuleFor(c => c.CartItems)
            .Must(items => items.Select(item => item.PriceCurrency).Distinct().Count() <= 1)
            .When(c => c.CartItems is not null)
            .WithMessage("All items in a cart must be priced in the same currency");

        RuleForEach(c => c.CartItems).ChildRules(item =>
        {
            CartItemRules.ProductId(item.RuleFor(i => i.ProductId));
            CartItemRules.ProductName(item.RuleFor(i => i.ProductName));
            CartItemRules.PriceAmount(item.RuleFor(i => i.PriceAmount));
            CartItemRules.PriceCurrency(item.RuleFor(i => i.PriceCurrency));
            CartItemRules.Quantity(item.RuleFor(i => i.Quantity));
        });
    }
}
