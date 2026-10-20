using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users;
using Identity.Application.Users.Login;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class LoginCommandHandlerTests
{
    private const string GoodCredential = "Correct-Secret1";
    private const string BadCredential = "Wrong-Secret1";

    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidCredentials()
    {
        Result<AuthTokens> result = await CreateHandler().Handle(
            new LoginCommand("nobody@example.com", GoodCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);
        _deps.SetupPasswordMatches(GoodCredential, user.PasswordHash);

        Result<AuthTokens> result = await CreateHandler().Handle(
            new LoginCommand(user.Email, BadCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ConfirmationRequiredAndEmailUnconfirmed_ReturnsEmailNotConfirmed()
    {
        _deps.AccountOptions.RequireConfirmedEmail = true;
        User user = TestData.CreateUser(emailConfirmed: false);
        _deps.SetupUserByEmail(user);
        _deps.SetupPasswordMatches(GoodCredential, user.PasswordHash);

        Result<AuthTokens> result = await CreateHandler().Handle(
            new LoginCommand(user.Email, GoodCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailNotConfirmed, result.Error);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ConfirmationRequiredAndEmailConfirmed_Succeeds()
    {
        _deps.AccountOptions.RequireConfirmedEmail = true;
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);
        _deps.SetupPasswordMatches(GoodCredential, user.PasswordHash);

        Result<AuthTokens> result = await CreateHandler().Handle(
            new LoginCommand(user.Email, GoodCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ConfirmationNotRequiredAndEmailUnconfirmed_Succeeds()
    {
        _deps.AccountOptions.RequireConfirmedEmail = false;
        User user = TestData.CreateUser(emailConfirmed: false);
        _deps.SetupUserByEmail(user);
        _deps.SetupPasswordMatches(GoodCredential, user.PasswordHash);

        Result<AuthTokens> result = await CreateHandler().Handle(
            new LoginCommand(user.Email, GoodCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidCredentials_AddsOneRefreshTokenInNewFamilyAndSaves()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);
        _deps.SetupPasswordMatches(GoodCredential, user.PasswordHash);

        Result<AuthTokens> result = await CreateHandler().Handle(
            new LoginCommand(user.Email, GoodCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        RefreshToken added = Assert.Single(_deps.AddedRefreshTokens);
        Assert.Equal(user.Id, added.UserId);
        Assert.NotEqual(Guid.Empty, added.FamilyId);
        Assert.False(added.IsRevoked);
        Assert.Equal(TestData.UtcNow, added.CreatedOnUtc);
        Assert.Equal(TestData.UtcNow.AddDays(7), added.ExpiresOnUtc);
        Assert.Equal(_deps.SecureTokens.Hash(result.Value.RefreshToken), added.TokenHash);
        Assert.NotEqual(result.Value.RefreshToken, added.TokenHash);

        Assert.Equal(TestData.AccessTokenValue, result.Value.AccessToken);
        Assert.Equal(TestData.UtcNow.AddMinutes(15), result.Value.AccessTokenExpiresOnUtc);
        Assert.Equal(TestData.UtcNow.AddDays(7), result.Value.RefreshTokenExpiresOnUtc);

        _deps.VerifySavedOnce();
        _deps.JwtService.Verify(j => j.CreateAccessToken(user), Times.Once);
    }

    [Fact]
    public async Task Handle_TwoLogins_StartTwoDifferentFamilies()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);
        _deps.SetupPasswordMatches(GoodCredential, user.PasswordHash);
        LoginCommandHandler handler = CreateHandler();

        await handler.Handle(new LoginCommand(user.Email, GoodCredential), TestContext.Current.CancellationToken);
        await handler.Handle(new LoginCommand(user.Email, GoodCredential), TestContext.Current.CancellationToken);

        Assert.Equal(2, _deps.AddedRefreshTokens.Count);
        Assert.NotEqual(_deps.AddedRefreshTokens[0].FamilyId, _deps.AddedRefreshTokens[1].FamilyId);
        Assert.NotEqual(_deps.AddedRefreshTokens[0].TokenHash, _deps.AddedRefreshTokens[1].TokenHash);
    }

    private LoginCommandHandler CreateHandler() =>
        new(
            _deps.Users.Object,
            _deps.PasswordHasher.Object,
            _deps.CreateAuthTokenIssuer(),
            _deps.UnitOfWork.Object,
            _deps.AccountOptionsAccessor);
}
