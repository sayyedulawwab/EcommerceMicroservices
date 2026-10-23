using FluentValidation;

namespace Ordering.Application.Orders.PlaceOrder;

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    private const int MaxItems = 100;
    private const int MaxQuantityPerItem = 1000;

    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty();

        RuleFor(c => c.OrderItems)
            .NotEmpty()
            .Must(items => items.Count <= MaxItems)
            .WithMessage($"An order can contain at most {MaxItems} items");

        RuleForEach(c => c.OrderItems).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, MaxQuantityPerItem);
        });
    }
}
