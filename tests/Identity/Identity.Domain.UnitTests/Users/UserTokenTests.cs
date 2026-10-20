using Identity.Domain.Users;
using SharedKernel.Domain;

namespace Identity.Domain.UnitTests.Users;

public sealed class UserTokenTests
{
    private const string RawToken = "raw-token-value";
    private const string TokenHash = "hashed-token-value";
    private static readonly DateTime NowUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiresOnUtc = NowUtc.AddHours(24);

    [Theory]
    [InlineData(UserTokenPurpose.EmailConfirmation)]
    [InlineData(UserTokenPurpose.PasswordReset)]
    public void Issue_ValidArguments_SetsProperties(UserTokenPurpose purpose)
    {
        User user = CreateUser();

        var token = UserToken.Issue(user, purpose, RawToken, TokenHash, ExpiresOnUtc, NowUtc);

        Assert.Same(user, token.User);
        Assert.Equal(user.Id, token.UserId);
        Assert.Equal(purpose, token.Purpose);
        Assert.Equal(TokenHash, token.TokenHash);
        Assert.Equal(ExpiresOnUtc, token.ExpiresOnUtc);
        Assert.Equal(NowUtc, token.CreatedOnUtc);
        Assert.Null(token.ConsumedOnUtc);
    }

    [Theory]
    [InlineData(UserTokenPurpose.EmailConfirmation)]
    [InlineData(UserTokenPurpose.PasswordReset)]
    public void Issue_ValidArguments_RaisesSingleUserTokenIssuedDomainEvent(UserTokenPurpose purpose)
    {
        User user = CreateUser();

        var token = UserToken.Issue(user, purpose, RawToken, TokenHash, ExpiresOnUtc, NowUtc);

        IDomainEvent domainEvent = Assert.Single(token.GetDomainEvents());
        UserTokenIssuedDomainEvent issued = Assert.IsType<UserTokenIssuedDomainEvent>(domainEvent);
        Assert.Equal("jane@example.com", issued.Email);
        Assert.Equal("Jane", issued.FirstName);
        Assert.Equal(purpose, issued.Purpose);
        Assert.Equal(RawToken, issued.RawToken);
    }

    [Fact]
    public void Issue_ValidArguments_EventDoesNotCarryTheHash()
    {
        var token = UserToken.Issue(CreateUser(), UserTokenPurpose.EmailConfirmation, RawToken, TokenHash, ExpiresOnUtc, NowUtc);

        UserTokenIssuedDomainEvent issued = Assert.IsType<UserTokenIssuedDomainEvent>(Assert.Single(token.GetDomainEvents()));
        Assert.NotEqual(TokenHash, issued.RawToken);
    }

    [Fact]
    public void IsUsable_FreshTokenBeforeExpiry_ReturnsTrue()
    {
        UserToken token = CreateToken();

        Assert.True(token.IsUsable(NowUtc.AddHours(1)));
    }

    [Fact]
    public void IsUsable_AtExpiry_ReturnsFalse()
    {
        UserToken token = CreateToken();

        Assert.False(token.IsUsable(ExpiresOnUtc));
    }

    [Fact]
    public void IsUsable_AfterExpiry_ReturnsFalse()
    {
        UserToken token = CreateToken();

        Assert.False(token.IsUsable(ExpiresOnUtc.AddMinutes(1)));
    }

    [Fact]
    public void IsUsable_ConsumedToken_ReturnsFalse()
    {
        UserToken token = CreateToken();

        token.Consume(NowUtc.AddMinutes(1));

        Assert.False(token.IsUsable(NowUtc.AddMinutes(2)));
    }

    [Fact]
    public void Consume_FreshToken_SetsConsumedOnUtc()
    {
        UserToken token = CreateToken();

        token.Consume(NowUtc.AddMinutes(1));

        Assert.Equal(NowUtc.AddMinutes(1), token.ConsumedOnUtc);
    }

    [Fact]
    public void Consume_AlreadyConsumedToken_KeepsFirstConsumptionTime()
    {
        UserToken token = CreateToken();

        token.Consume(NowUtc.AddMinutes(1));
        token.Consume(NowUtc.AddMinutes(10));

        Assert.Equal(NowUtc.AddMinutes(1), token.ConsumedOnUtc);
    }

    private static User CreateUser() =>
        User.Create("Jane", "Doe", "Jane@Example.com", "stored-hash", isAdmin: false, NowUtc);

    private static UserToken CreateToken() =>
        UserToken.Issue(CreateUser(), UserTokenPurpose.EmailConfirmation, RawToken, TokenHash, ExpiresOnUtc, NowUtc);
}
