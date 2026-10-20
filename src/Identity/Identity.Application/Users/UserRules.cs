using FluentValidation;

namespace Identity.Application.Users;

internal static class UserRules
{
    public static void Email<T>(IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(200).EmailAddress();

    public static void Name<T>(IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(200);

    public static void NewPassword<T>(IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("The password must contain an upper-case letter")
            .Matches("[a-z]").WithMessage("The password must contain a lower-case letter")
            .Matches("[0-9]").WithMessage("The password must contain a digit");
}
