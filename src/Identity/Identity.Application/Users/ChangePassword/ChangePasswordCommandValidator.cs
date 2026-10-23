using FluentValidation;

namespace Identity.Application.Users.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.CurrentPassword).NotEmpty();
        UserRules.NewPassword(RuleFor(c => c.NewPassword));

        RuleFor(c => c.NewPassword)
            .NotEqual(c => c.CurrentPassword)
            .WithMessage("The new password must be different from the current one");
    }
}
