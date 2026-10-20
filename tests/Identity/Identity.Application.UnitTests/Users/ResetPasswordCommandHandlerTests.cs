using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users.ResetPassword;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class ResetPasswordCommandHandlerTests
{
    private const string MailedToken = "mailed-reset-token";
    private const string ReplacementCredential = "Replacement-Secret1";
    private const string ReplacementHash = "replacement-hash";

    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidOrExpired()
    {
        Result result = await CreateHandler().Handle(
            new ResetPasswordCommand("nobody@example.com", MailedToken, ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        _deps.RefreshTokens.VerifyNoOtherCalls();
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsInvalidOrExpiredAndKeepsPassword()
    {
        User user = TestData.CreateUser();
        _deps.SetupUserByEmail(user);

        Result result = await CreateHandler().Handle(
            new ResetPasswordCommand(user.Email, MailedToken, ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        Assert.Equal(TestData.StoredHash, user.PasswordHash);
        _deps.RefreshTokens.VerifyNoOtherCalls();
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsInvalidOrExpiredAndKeepsPassword()
    {
        User user = TestData.CreateUser();
        _deps.SetupUserByEmail(user);
        SetupStoredToken(user, TestData.UtcNow.AddMinutes(-1));

        Result result = await CreateHandler().Handle(
            new ResetPasswordCommand(user.Email, MailedToken, ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        Assert.Equal(TestData.StoredHash, user.PasswordHash);
        _deps.RefreshTokens.VerifyNoOtherCalls();
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ConsumedToken_ReturnsInvalidOrExpired()
    {
        User user = TestData.CreateUser();
        _deps.SetupUserByEmail(user);
        UserToken token = SetupStoredToken(user, TestData.UtcNow.AddMinutes(30));
        token.Consume(TestData.UtcNow.AddMinutes(-1));

        Result result = await CreateHandler().Handle(
            new ResetPasswordCommand(user.Email, MailedToken, ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        Assert.Equal(TestData.StoredHash, user.PasswordHash);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ValidToken_ConsumesTokenChangesHashConfirmsEmailAndRevokesSessions()
    {
        User user = TestData.CreateUser(emailConfirmed: false);
        _deps.SetupUserByEmail(user);
        _deps.SetupPasswordHashing(ReplacementCredential, ReplacementHash);
        UserToken token = SetupStoredToken(user, TestData.UtcNow.AddMinutes(30));

        Result result = await CreateHandler().Handle(
            new ResetPasswordCommand(user.Email, MailedToken, ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestData.UtcNow, token.ConsumedOnUtc);
        Assert.Equal(ReplacementHash, user.PasswordHash);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(TestData.UtcNow, user.UpdatedOnUtc);
        _deps.UserTokens.Verify(
            r => r.GetAsync(user.Id, UserTokenPurpose.PasswordReset, _deps.SecureTokens.Hash(MailedToken), It.IsAny<CancellationToken>()),
            Times.Once);
        _deps.RefreshTokens.Verify(
            r => r.RevokeAllForUserAsync(user.Id, TestData.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);
        _deps.VerifySavedOnce();
    }

    private UserToken SetupStoredToken(User user, DateTime expiresOnUtc)
    {
        string hash = _deps.SecureTokens.Hash(MailedToken);
        var token = UserToken.Issue(user, UserTokenPurpose.PasswordReset, MailedToken, hash, expiresOnUtc, TestData.UtcNow.AddMinutes(-30));

        _deps.UserTokens
            .Setup(r => r.GetAsync(user.Id, UserTokenPurpose.PasswordReset, hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        return token;
    }

    private ResetPasswordCommandHandler CreateHandler() =>
        new(
            _deps.Users.Object,
            _deps.UserTokens.Object,
            _deps.RefreshTokens.Object,
            _deps.SecureTokens,
            _deps.PasswordHasher.Object,
            _deps.UnitOfWork.Object,
            _deps.Clock);
}
