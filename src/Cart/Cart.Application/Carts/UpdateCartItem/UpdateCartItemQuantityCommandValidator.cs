using FluentValidation;

namespace Cart.Application.Carts.UpdateCartItem;

public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        CartItemRules.ProductId(RuleFor(c => c.ProductId));
        RuleFor(c => c.Quantity).InclusiveBetween(0, CartItemRules.MaxQuantity);
    }
}
