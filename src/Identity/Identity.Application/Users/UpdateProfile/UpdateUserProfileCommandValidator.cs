using FluentValidation;

namespace Identity.Application.Users.UpdateProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        UserRules.Name(RuleFor(c => c.FirstName));
        UserRules.Name(RuleFor(c => c.LastName));
    }
}
