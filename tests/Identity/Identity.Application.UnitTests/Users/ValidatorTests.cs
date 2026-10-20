using FluentValidation.Results;
using Identity.Application.Users.ChangePassword;
using Identity.Application.Users.Register;

namespace Identity.Application.UnitTests.Users;

public sealed class ValidatorTests
{
    private const string StrongCredential = "Strong-Secret1";
    private const string OtherStrongCredential = "Other-Secret2";

    private readonly RegisterUserCommandValidator _registerValidator = new();
    private readonly ChangePasswordCommandValidator _changePasswordValidator = new();

    [Fact]
    public void RegisterUser_ValidCommand_IsValid()
    {
        ValidationResult result = _registerValidator.Validate(
            new RegisterUserCommand("Jane", "Doe", "jane@example.com", StrongCredential));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ab1defg")]
    [InlineData("alllower1")]
    [InlineData("ALLUPPER1")]
    [InlineData("NoDigitsHere")]
    public void RegisterUser_WeakPassword_FailsOnPassword(string candidate)
    {
        ValidationResult result = _registerValidator.Validate(
            new RegisterUserCommand("Jane", "Doe", "jane@example.com", candidate));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void RegisterUser_PasswordOfExactlyEightCharacters_IsValid()
    {
        ValidationResult result = _registerValidator.Validate(
            new RegisterUserCommand("Jane", "Doe", "jane@example.com", "Abcdef1g"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("jane@")]
    public void RegisterUser_InvalidEmail_FailsOnEmail(string email)
    {
        ValidationResult result = _registerValidator.Validate(
            new RegisterUserCommand("Jane", "Doe", email, StrongCredential));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Fact]
    public void RegisterUser_EmptyNames_FailsOnNames()
    {
        ValidationResult result = _registerValidator.Validate(
            new RegisterUserCommand("", "", "jane@example.com", StrongCredential));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.FirstName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserCommand.LastName));
    }

    [Fact]
    public void ChangePassword_NewEqualsCurrent_FailsOnNewPassword()
    {
        ValidationResult result = _changePasswordValidator.Validate(
            new ChangePasswordCommand(1, StrongCredential, StrongCredential));

        Assert.False(result.IsValid);
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(ChangePasswordCommand.NewPassword), failure.PropertyName);
        Assert.Equal("The new password must be different from the current one", failure.ErrorMessage);
    }

    [Fact]
    public void ChangePassword_DifferentStrongPassword_IsValid()
    {
        ValidationResult result = _changePasswordValidator.Validate(
            new ChangePasswordCommand(1, StrongCredential, OtherStrongCredential));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ChangePassword_WeakNewPassword_FailsOnNewPassword()
    {
        ValidationResult result = _changePasswordValidator.Validate(
            new ChangePasswordCommand(1, StrongCredential, "weak"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void ChangePassword_NonPositiveUserId_FailsOnUserId()
    {
        ValidationResult result = _changePasswordValidator.Validate(
            new ChangePasswordCommand(0, StrongCredential, OtherStrongCredential));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordCommand.UserId));
    }
}
