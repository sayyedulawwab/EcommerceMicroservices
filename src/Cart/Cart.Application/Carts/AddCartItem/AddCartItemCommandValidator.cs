using FluentValidation;

namespace Cart.Application.Carts.AddCartItem;

public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        CartItemRules.ProductId(RuleFor(c => c.ProductId));
        CartItemRules.ProductName(RuleFor(c => c.ProductName));
        CartItemRules.PriceAmount(RuleFor(c => c.PriceAmount));
        CartItemRules.PriceCurrency(RuleFor(c => c.PriceCurrency));
        CartItemRules.Quantity(RuleFor(c => c.Quantity));
    }
}
