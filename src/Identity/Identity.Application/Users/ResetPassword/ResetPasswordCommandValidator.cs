using FluentValidation;

namespace Identity.Application.Users.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Token).NotEmpty();
        UserRules.NewPassword(RuleFor(c => c.NewPassword));
    }
}
