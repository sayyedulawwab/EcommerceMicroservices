using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Email;
using Identity.Application.Users.Notifications;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.Application.UnitTests.Users;

public sealed class UserTokenIssuedDomainEventHandlerTests
{
    // Characters that must be escaped in a query string: '+' and '@' in the email, '/', '+' and '=' in the token.
    private const string Email = "jane+shop@example.com";
    private const string EscapedEmail = "jane%2Bshop%40example.com";
    private const string RawToken = "ab/c+d=";
    private const string EscapedToken = "ab%2Fc%2Bd%3D";

    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly List<(string To, string Subject, string Body)> _sentEmails = [];

    private readonly AccountOptions _accountOptions = new()
    {
        FrontendBaseUrl = "https://shop.test",
        EmailConfirmationTokenLifetimeHours = 24,
        PasswordResetTokenLifetimeMinutes = 60,
    };

    public UserTokenIssuedDomainEventHandlerTests() =>
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((to, subject, body, _) => _sentEmails.Add((to, subject, body)))
            .Returns(Task.CompletedTask);

    [Fact]
    public async Task Handle_EmailConfirmation_SendsOneEmailWithConfirmLink()
    {
        await CreateHandler().Handle(
            new UserTokenIssuedDomainEvent(Email, "Jane", UserTokenPurpose.EmailConfirmation, RawToken),
            TestContext.Current.CancellationToken);

        (string to, string subject, string body) = Assert.Single(_sentEmails);
        Assert.Equal(Email, to);
        Assert.Equal("Confirm your email address", subject);
        Assert.Contains($"https://shop.test/confirm-email?email={EscapedEmail}&token={EscapedToken}", body, StringComparison.Ordinal);
        Assert.DoesNotContain("/reset-password", body, StringComparison.Ordinal);
        Assert.Contains("Jane", body, StringComparison.Ordinal);
        Assert.Contains("24 hours", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_PasswordReset_SendsOneEmailWithResetLink()
    {
        await CreateHandler().Handle(
            new UserTokenIssuedDomainEvent(Email, "Jane", UserTokenPurpose.PasswordReset, RawToken),
            TestContext.Current.CancellationToken);

        (string to, string subject, string body) = Assert.Single(_sentEmails);
        Assert.Equal(Email, to);
        Assert.Equal("Reset your password", subject);
        Assert.Contains($"https://shop.test/reset-password?email={EscapedEmail}&token={EscapedToken}", body, StringComparison.Ordinal);
        Assert.DoesNotContain("/confirm-email", body, StringComparison.Ordinal);
        Assert.Contains("60 minutes", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_BaseUrlWithTrailingSlash_DoesNotProduceDoubleSlash()
    {
        _accountOptions.FrontendBaseUrl = "https://shop.test/";

        await CreateHandler().Handle(
            new UserTokenIssuedDomainEvent(Email, "Jane", UserTokenPurpose.EmailConfirmation, RawToken),
            TestContext.Current.CancellationToken);

        (_, _, string body) = Assert.Single(_sentEmails);
        Assert.Contains("https://shop.test/confirm-email?", body, StringComparison.Ordinal);
        Assert.DoesNotContain("shop.test//", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_AnyPurpose_PassesCancellationTokenToSender()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await CreateHandler().Handle(
            new UserTokenIssuedDomainEvent(Email, "Jane", UserTokenPurpose.PasswordReset, RawToken),
            cancellationToken);

        _emailSender.Verify(
            s => s.SendAsync(Email, It.IsAny<string>(), It.IsAny<string>(), cancellationToken),
            Times.Once);
    }

    private UserTokenIssuedDomainEventHandler CreateHandler() =>
        new(_emailSender.Object, Options.Create(_accountOptions));
}
