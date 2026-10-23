using FluentValidation;

namespace Ordering.Application.Orders.CancelOrder;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(c => c.OrderId)
            .NotEmpty();

        RuleFor(c => c.Reason)
            .NotEmpty();
    }
}
