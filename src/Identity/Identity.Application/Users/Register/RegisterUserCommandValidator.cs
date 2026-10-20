using FluentValidation;

namespace Identity.Application.Users.Register;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        UserRules.Name(RuleFor(c => c.FirstName));
        UserRules.Name(RuleFor(c => c.LastName));
        UserRules.Email(RuleFor(c => c.Email));
        UserRules.NewPassword(RuleFor(c => c.Password));
    }
}
