using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users.ForgotPassword;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_UnknownEmail_SucceedsAndIssuesNothing()
    {
        Result result = await CreateHandler().Handle(
            new ForgotPasswordCommand("nobody@example.com"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(_deps.AddedUserTokens);
        _deps.UserTokens.Verify(
            r => r.ConsumeAllAsync(It.IsAny<long>(), It.IsAny<UserTokenPurpose>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_KnownEmail_ConsumesOutstandingTokensThenIssuesPasswordResetToken()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);

        Result result = await CreateHandler().Handle(
            new ForgotPasswordCommand(user.Email),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _deps.UserTokens.Verify(
            r => r.ConsumeAllAsync(user.Id, UserTokenPurpose.PasswordReset, TestData.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);

        UserToken token = Assert.Single(_deps.AddedUserTokens);
        Assert.Equal(UserTokenPurpose.PasswordReset, token.Purpose);
        Assert.Equal(user.Id, token.UserId);
        Assert.Equal(TestData.UtcNow.AddMinutes(60), token.ExpiresOnUtc);

        UserTokenIssuedDomainEvent issued = Assert.IsType<UserTokenIssuedDomainEvent>(Assert.Single(token.GetDomainEvents()));
        Assert.Equal(UserTokenPurpose.PasswordReset, issued.Purpose);
        Assert.Equal(_deps.SecureTokens.Hash(issued.RawToken), token.TokenHash);

        Assert.Equal(
            [nameof(IUserTokenRepository.ConsumeAllAsync), nameof(IUserTokenRepository.Add), nameof(IUnitOfWork.SaveChangesAsync)],
            _deps.CallOrder);
    }

    private ForgotPasswordCommandHandler CreateHandler() =>
        new(
            _deps.Users.Object,
            _deps.UserTokens.Object,
            _deps.CreateUserTokenIssuer(),
            _deps.UnitOfWork.Object,
            _deps.Clock);
}
