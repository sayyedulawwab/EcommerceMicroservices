using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users;
using Identity.Application.Users.RefreshTokens;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class RefreshTokensCommandHandlerTests
{
    private const string PresentedToken = "presented-refresh-token";

    private readonly TestDependencies _deps = new();
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly User _user = TestData.CreateUser(emailConfirmed: true);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_MissingOrBlankToken_ReturnsInvalid(string? token)
    {
        Result<AuthTokens> result = await CreateHandler().Handle(
            new RefreshTokensCommand(token),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.Invalid, result.Error);
        _deps.RefreshTokens.Verify(
            r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsInvalid()
    {
        Result<AuthTokens> result = await CreateHandler().Handle(
            new RefreshTokensCommand("unknown-token"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.Invalid, result.Error);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_ValidToken_LooksTokenUpByItsHash()
    {
        SetupStoredToken();

        await CreateHandler().Handle(new RefreshTokensCommand(PresentedToken), TestContext.Current.CancellationToken);

        _deps.RefreshTokens.Verify(
            r => r.GetByTokenHashAsync(_deps.SecureTokens.Hash(PresentedToken), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesOldTokenAndIssuesNewOneInSameFamily()
    {
        RefreshToken stored = SetupStoredToken();

        Result<AuthTokens> result = await CreateHandler().Handle(
            new RefreshTokensCommand(PresentedToken),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        Assert.True(stored.IsRevoked);
        Assert.Equal(TestData.UtcNow, stored.RevokedOnUtc);

        RefreshToken added = Assert.Single(_deps.AddedRefreshTokens);
        Assert.Equal(_familyId, added.FamilyId);
        Assert.Equal(_user.Id, added.UserId);
        Assert.False(added.IsRevoked);
        Assert.Equal(TestData.UtcNow.AddDays(7), added.ExpiresOnUtc);
        Assert.Equal(_deps.SecureTokens.Hash(result.Value.RefreshToken), added.TokenHash);
        Assert.NotEqual(PresentedToken, result.Value.RefreshToken);
        Assert.NotEqual(stored.TokenHash, added.TokenHash);
        Assert.Equal(TestData.AccessTokenValue, result.Value.AccessToken);

        _deps.RefreshTokens.Verify(
            r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deps.VerifySavedOnce();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsInvalid()
    {
        SetupStoredToken(expiresOnUtc: TestData.UtcNow.AddSeconds(-1));

        Result<AuthTokens> result = await CreateHandler().Handle(
            new RefreshTokensCommand(PresentedToken),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.Invalid, result.Error);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.RefreshTokens.Verify(
            r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deps.VerifyNeverSaved();
    }

    [Theory]
    [InlineData(31)]
    [InlineData(3600)]
    public async Task Handle_TokenRevokedMoreThan30SecondsAgo_RevokesFamilyAndReturnsReuseDetected(int secondsSinceRevocation)
    {
        RefreshToken stored = SetupStoredToken();
        stored.Revoke(TestData.UtcNow.AddSeconds(-secondsSinceRevocation));

        Result<AuthTokens> result = await CreateHandler().Handle(
            new RefreshTokensCommand(PresentedToken),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.ReuseDetected, result.Error);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.RefreshTokens.Verify(
            r => r.RevokeFamilyAsync(_familyId, TestData.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);
        _deps.JwtService.Verify(j => j.CreateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(29)]
    public async Task Handle_TokenRevokedWithinGracePeriod_ReturnsInvalidWithoutRevokingFamily(int secondsSinceRevocation)
    {
        RefreshToken stored = SetupStoredToken();
        stored.Revoke(TestData.UtcNow.AddSeconds(-secondsSinceRevocation));

        Result<AuthTokens> result = await CreateHandler().Handle(
            new RefreshTokensCommand(PresentedToken),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(RefreshTokenErrors.Invalid, result.Error);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.RefreshTokens.Verify(
            r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_RevokedAndExpiredTokenReplayedLater_StillTreatedAsReuse()
    {
        RefreshToken stored = SetupStoredToken(expiresOnUtc: TestData.UtcNow.AddDays(-1));
        stored.Revoke(TestData.UtcNow.AddDays(-2));

        Result<AuthTokens> result = await CreateHandler().Handle(
            new RefreshTokensCommand(PresentedToken),
            TestContext.Current.CancellationToken);

        Assert.Equal(RefreshTokenErrors.ReuseDetected, result.Error);
        _deps.RefreshTokens.Verify(
            r => r.RevokeFamilyAsync(_familyId, TestData.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private RefreshToken SetupStoredToken(DateTime? expiresOnUtc = null)
    {
        string hash = _deps.SecureTokens.Hash(PresentedToken);

        RefreshToken stored = RefreshToken
            .Create(hash, _user.Id, _familyId, expiresOnUtc ?? TestData.UtcNow.AddDays(3), TestData.UtcNow.AddDays(-4))
            .WithId(100)
            .WithUser(_user);

        _deps.RefreshTokens
            .Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        return stored;
    }

    private RefreshTokensCommandHandler CreateHandler() =>
        new(
            _deps.RefreshTokens.Object,
            _deps.SecureTokens,
            _deps.CreateAuthTokenIssuer(),
            _deps.UnitOfWork.Object,
            _deps.Clock);
}
