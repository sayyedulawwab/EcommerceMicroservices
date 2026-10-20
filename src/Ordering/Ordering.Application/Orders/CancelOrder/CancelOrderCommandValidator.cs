using FluentValidation;

namespace Ordering.Application.Orders.CancelOrder;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(c => c.OrderId)
            .GreaterThan(0);

        RuleFor(c => c.Reason)
            .NotEmpty();
    }
}
