using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users.Register;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class RegisterUserCommandHandlerTests
{
    private const string ChosenCredential = "Chosen-Secret1";
    private const string HashedCredential = "hashed-credential";

    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ReturnsAlreadyExists()
    {
        User existing = TestData.CreateUser();
        _deps.SetupUserByEmail(existing);

        Result<long> result = await CreateHandler().Handle(
            new RegisterUserCommand("Jane", "Doe", existing.Email, ChosenCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.AlreadyExists, result.Error);
        _deps.Users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        Assert.Empty(_deps.AddedUserTokens);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_NewEmail_AddsUserWithHashedPasswordAndNormalizedEmail()
    {
        _deps.SetupPasswordHashing(ChosenCredential, HashedCredential);
        User? addedUser = null;
        _deps.Users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => addedUser = u);

        Result<long> result = await CreateHandler().Handle(
            new RegisterUserCommand("Jane", "Doe", "  Jane@Example.COM ", ChosenCredential),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(addedUser);
        Assert.Equal("Jane", addedUser.FirstName);
        Assert.Equal("Doe", addedUser.LastName);
        Assert.Equal("jane@example.com", addedUser.Email);
        Assert.Equal(HashedCredential, addedUser.PasswordHash);
        Assert.False(addedUser.IsAdmin);
        Assert.False(addedUser.EmailConfirmed);
        Assert.Equal(TestData.UtcNow, addedUser.CreatedOnUtc);
        Assert.Equal(addedUser.Id, result.Value);
    }

    [Fact]
    public async Task Handle_NewEmail_IssuesEmailConfirmationTokenAndSavesOnce()
    {
        _deps.SetupPasswordHashing(ChosenCredential, HashedCredential);
        User? addedUser = null;
        _deps.Users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => addedUser = u);

        await CreateHandler().Handle(
            new RegisterUserCommand("Jane", "Doe", TestData.Email, ChosenCredential),
            TestContext.Current.CancellationToken);

        UserToken token = Assert.Single(_deps.AddedUserTokens);
        Assert.Equal(UserTokenPurpose.EmailConfirmation, token.Purpose);
        Assert.Same(addedUser, token.User);
        Assert.Equal(TestData.UtcNow, token.CreatedOnUtc);
        Assert.Equal(TestData.UtcNow.AddHours(24), token.ExpiresOnUtc);
        Assert.Null(token.ConsumedOnUtc);

        UserTokenIssuedDomainEvent issued = Assert.IsType<UserTokenIssuedDomainEvent>(Assert.Single(token.GetDomainEvents()));
        Assert.Equal(TestData.Email, issued.Email);
        Assert.Equal(UserTokenPurpose.EmailConfirmation, issued.Purpose);
        Assert.Equal(_deps.SecureTokens.Hash(issued.RawToken), token.TokenHash);
        Assert.NotEqual(issued.RawToken, token.TokenHash);

        _deps.VerifySavedOnce();
        Assert.Equal(nameof(IUnitOfWork.SaveChangesAsync), _deps.CallOrder[^1]);
    }

    private RegisterUserCommandHandler CreateHandler() =>
        new(
            _deps.PasswordHasher.Object,
            _deps.Users.Object,
            _deps.CreateUserTokenIssuer(),
            _deps.UnitOfWork.Object,
            _deps.Clock);
}
