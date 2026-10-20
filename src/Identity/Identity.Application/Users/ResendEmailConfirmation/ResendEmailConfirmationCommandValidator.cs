using FluentValidation;

namespace Identity.Application.Users.ResendEmailConfirmation;

public class ResendEmailConfirmationCommandValidator : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationCommandValidator()
    {
        UserRules.Email(RuleFor(c => c.Email));
    }
}
