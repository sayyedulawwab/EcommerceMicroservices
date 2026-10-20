using FluentValidation;

namespace Identity.Application.Users.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        UserRules.Email(RuleFor(c => c.Email));
    }
}
