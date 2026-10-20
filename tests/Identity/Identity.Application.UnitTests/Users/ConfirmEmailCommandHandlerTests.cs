using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users.ConfirmEmail;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class ConfirmEmailCommandHandlerTests
{
    private const string MailedToken = "mailed-token";

    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidOrExpired()
    {
        Result result = await CreateHandler().Handle(
            new ConfirmEmailCommand("nobody@example.com", MailedToken),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedWithTheMailedToken_SucceedsWithoutSaving()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);
        SetupStoredToken(user, TestData.UtcNow.AddHours(1));

        Result result = await CreateHandler().Handle(
            new ConfirmEmailCommand(user.Email, MailedToken),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedWithAWrongToken_ReturnsInvalidOrExpired_SoAccountsCantBeProbed()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);

        Result result = await CreateHandler().Handle(
            new ConfirmEmailCommand(user.Email, MailedToken),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsInvalidOrExpired()
    {
        User user = TestData.CreateUser();
        _deps.SetupUserByEmail(user);

        Result result = await CreateHandler().Handle(
            new ConfirmEmailCommand(user.Email, MailedToken),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        Assert.False(user.EmailConfirmed);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsInvalidOrExpired()
    {
        User user = TestData.CreateUser();
        _deps.SetupUserByEmail(user);
        UserToken token = SetupStoredToken(user, TestData.UtcNow.AddSeconds(-1));

        Result result = await CreateHandler().Handle(
            new ConfirmEmailCommand(user.Email, MailedToken),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        Assert.False(user.EmailConfirmed);
        Assert.Null(token.ConsumedOnUtc);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ConsumedToken_ReturnsInvalidOrExpired()
    {
        User user = TestData.CreateUser();
        _deps.SetupUserByEmail(user);
        UserToken token = SetupStoredToken(user, TestData.UtcNow.AddHours(1));
        token.Consume(TestData.UtcNow.AddMinutes(-5));

        Result result = await CreateHandler().Handle(
            new ConfirmEmailCommand(user.Email, MailedToken),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserTokenErrors.InvalidOrExpired, result.Error);
        Assert.False(user.EmailConfirmed);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ValidToken_ConsumesTokenConfirmsEmailAndSaves()
    {
        User user = TestData.CreateUser();
        _deps.SetupUserByEmail(user);
        UserToken token = SetupStoredToken(user, TestData.UtcNow.AddHours(1));

        Result result = await CreateHandler().Handle(
            new ConfirmEmailCommand(user.Email, MailedToken),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(TestData.UtcNow, token.ConsumedOnUtc);
        _deps.UserTokens.Verify(
            r => r.GetAsync(user.Id, UserTokenPurpose.EmailConfirmation, _deps.SecureTokens.Hash(MailedToken), It.IsAny<CancellationToken>()),
            Times.Once);
        _deps.VerifySavedOnce();
    }

    private UserToken SetupStoredToken(User user, DateTime expiresOnUtc)
    {
        string hash = _deps.SecureTokens.Hash(MailedToken);
        var token = UserToken.Issue(user, UserTokenPurpose.EmailConfirmation, MailedToken, hash, expiresOnUtc, TestData.UtcNow.AddHours(-2));

        _deps.UserTokens
            .Setup(r => r.GetAsync(user.Id, UserTokenPurpose.EmailConfirmation, hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        return token;
    }

    private ConfirmEmailCommandHandler CreateHandler() =>
        new(
            _deps.Users.Object,
            _deps.UserTokens.Object,
            _deps.SecureTokens,
            _deps.UnitOfWork.Object,
            _deps.Clock);
}
