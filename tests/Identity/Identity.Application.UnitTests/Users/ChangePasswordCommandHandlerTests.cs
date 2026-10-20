using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users;
using Identity.Application.Users.ChangePassword;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class ChangePasswordCommandHandlerTests
{
    private const string CurrentCredential = "Current-Secret1";
    private const string ReplacementCredential = "Replacement-Secret1";
    private const string ReplacementHash = "replacement-hash";

    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        Result<AuthTokens> result = await CreateHandler().Handle(
            new ChangePasswordCommand(99, CurrentCredential, ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.Equal(UserErrors.NotFound, result.Error);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsInvalidCurrentPassword()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserById(user);
        _deps.SetupPasswordMatches(CurrentCredential, user.PasswordHash);

        Result<AuthTokens> result = await CreateHandler().Handle(
            new ChangePasswordCommand(user.Id, "Not-The-Current1", ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.InvalidCurrentPassword, result.Error);
        Assert.Equal(TestData.StoredHash, user.PasswordHash);
        Assert.Empty(_deps.AddedRefreshTokens);
        _deps.RefreshTokens.Verify(
            r => r.RevokeAllForUserAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_ChangesHashRevokesSessionsAndIssuesTokensInNewFamily()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserById(user);
        _deps.SetupPasswordMatches(CurrentCredential, user.PasswordHash);
        _deps.SetupPasswordHashing(ReplacementCredential, ReplacementHash);

        Result<AuthTokens> result = await CreateHandler().Handle(
            new ChangePasswordCommand(user.Id, CurrentCredential, ReplacementCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReplacementHash, user.PasswordHash);
        Assert.Equal(TestData.UtcNow, user.UpdatedOnUtc);

        _deps.RefreshTokens.Verify(
            r => r.RevokeAllForUserAsync(user.Id, TestData.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);

        RefreshToken added = Assert.Single(_deps.AddedRefreshTokens);
        Assert.NotEqual(Guid.Empty, added.FamilyId);
        Assert.Equal(user.Id, added.UserId);
        Assert.False(added.IsRevoked);
        Assert.Equal(_deps.SecureTokens.Hash(result.Value.RefreshToken), added.TokenHash);
        Assert.Equal(TestData.AccessTokenValue, result.Value.AccessToken);

        // Add only tracks the new token; the revoke-all UPDATE runs before the save inserts it, so the new session survives.
        Assert.Equal(
            [nameof(IRefreshTokenRepository.Add), nameof(IRefreshTokenRepository.RevokeAllForUserAsync), nameof(IUnitOfWork.SaveChangesAsync)],
            _deps.CallOrder);
    }

    [Fact]
    public async Task Handle_CalledTwice_EachCallStartsADifferentFamily()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserById(user);
        _deps.PasswordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _deps.SetupPasswordHashing(ReplacementCredential, ReplacementHash);
        ChangePasswordCommandHandler handler = CreateHandler();

        await handler.Handle(new ChangePasswordCommand(user.Id, CurrentCredential, ReplacementCredential), TestContext.Current.CancellationToken);
        await handler.Handle(new ChangePasswordCommand(user.Id, CurrentCredential, ReplacementCredential), TestContext.Current.CancellationToken);

        Assert.Equal(2, _deps.AddedRefreshTokens.Count);
        Assert.NotEqual(_deps.AddedRefreshTokens[0].FamilyId, _deps.AddedRefreshTokens[1].FamilyId);
    }

    private ChangePasswordCommandHandler CreateHandler() =>
        new(
            _deps.Users.Object,
            _deps.RefreshTokens.Object,
            _deps.PasswordHasher.Object,
            _deps.CreateAuthTokenIssuer(),
            _deps.UnitOfWork.Object,
            _deps.Clock);
}
