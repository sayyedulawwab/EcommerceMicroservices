using Identity.Application.Abstractions.Auth;
using Identity.Application.Users;
using Identity.Domain.Users;
using Identity.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.TestUtilities;

/// <summary>Mocks shared by the handler tests. Added refresh/user tokens are captured so tests can inspect them.</summary>
internal sealed class TestDependencies
{
    public TestDependencies()
    {
        JwtService
            .Setup(j => j.CreateAccessToken(It.IsAny<User>()))
            .Returns(new AccessToken(TestData.AccessTokenValue, TestData.UtcNow.AddMinutes(15)));

        RefreshTokens
            .Setup(r => r.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token =>
            {
                AddedRefreshTokens.Add(token);
                CallOrder.Add(nameof(IRefreshTokenRepository.Add));
            });

        RefreshTokens
            .Setup(r => r.RevokeAllForUserAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => CallOrder.Add(nameof(IRefreshTokenRepository.RevokeAllForUserAsync)))
            .Returns(Task.CompletedTask);

        UserTokens
            .Setup(r => r.Add(It.IsAny<UserToken>()))
            .Callback<UserToken>(token =>
            {
                AddedUserTokens.Add(token);
                CallOrder.Add(nameof(IUserTokenRepository.Add));
            });

        UserTokens
            .Setup(r => r.ConsumeAllAsync(It.IsAny<long>(), It.IsAny<UserTokenPurpose>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => CallOrder.Add(nameof(IUserTokenRepository.ConsumeAllAsync)))
            .Returns(Task.CompletedTask);

        UnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => CallOrder.Add(nameof(IUnitOfWork.SaveChangesAsync)))
            .ReturnsAsync(1);

        // Run the transactional work straight away, like the real DbContext does.
        UnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken));
    }

    public Mock<IUserRepository> Users { get; } = new();
    public Mock<IRefreshTokenRepository> RefreshTokens { get; } = new();
    public Mock<IUserTokenRepository> UserTokens { get; } = new();
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IPasswordHasher> PasswordHasher { get; } = new();
    public Mock<IJwtService> JwtService { get; } = new();
    public SecureTokenService SecureTokens { get; } = new();
    public FixedDateTimeProvider Clock { get; } = new(TestData.UtcNow);

    public AccountOptions AccountOptions { get; } = new()
    {
        FrontendBaseUrl = "https://shop.test",
        RequireConfirmedEmail = false,
        RefreshTokenLifetimeDays = 7,
        EmailConfirmationTokenLifetimeHours = 24,
        PasswordResetTokenLifetimeMinutes = 60,
    };

    public List<RefreshToken> AddedRefreshTokens { get; } = [];
    public List<UserToken> AddedUserTokens { get; } = [];
    public List<string> CallOrder { get; } = [];

    public IOptions<AccountOptions> AccountOptionsAccessor => Options.Create(AccountOptions);

    public AuthTokenIssuer CreateAuthTokenIssuer() =>
        new(JwtService.Object, SecureTokens, RefreshTokens.Object, Clock, AccountOptionsAccessor);

    public UserTokenIssuer CreateUserTokenIssuer() =>
        new(SecureTokens, UserTokens.Object, Clock, AccountOptionsAccessor);

    public void SetupUserByEmail(User user) =>
        Users.Setup(r => r.GetByEmail(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    public void SetupUserById(User user) =>
        Users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    public void SetupPasswordMatches(string password, string storedHash) =>
        PasswordHasher.Setup(h => h.Verify(password, storedHash)).Returns(true);

    public void SetupPasswordHashing(string password, string resultingHash)
    {
        PasswordHasher.Setup(h => h.GenerateSalt()).Returns("salt");
        PasswordHasher.Setup(h => h.Hash(password, "salt")).Returns(resultingHash);
    }

    public void VerifySavedOnce() => UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyNeverSaved() => UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}
