using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users.Logout;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class LogoutCommandHandlerTests
{
    private const string PresentedToken = "presented-refresh-token";

    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_KnownToken_RevokesItsFamily()
    {
        var familyId = Guid.NewGuid();
        string hash = _deps.SecureTokens.Hash(PresentedToken);
        var stored = RefreshToken.Create(hash, TestData.UserId, familyId, TestData.UtcNow.AddDays(3), TestData.UtcNow.AddDays(-1));
        _deps.RefreshTokens
            .Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        Result result = await CreateHandler().Handle(new LogoutCommand(PresentedToken), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _deps.RefreshTokens.Verify(
            r => r.RevokeFamilyAsync(familyId, TestData.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);
        _deps.RefreshTokens.Verify(
            r => r.RevokeAllForUserAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_NoToken_SucceedsWithoutTouchingRepository(string? token)
    {
        Result result = await CreateHandler().Handle(new LogoutCommand(token), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _deps.RefreshTokens.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UnknownToken_SucceedsWithoutRevoking()
    {
        Result result = await CreateHandler().Handle(new LogoutCommand("unknown-token"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _deps.RefreshTokens.Verify(
            r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private LogoutCommandHandler CreateHandler() =>
        new(_deps.RefreshTokens.Object, _deps.SecureTokens, _deps.Clock);
}
