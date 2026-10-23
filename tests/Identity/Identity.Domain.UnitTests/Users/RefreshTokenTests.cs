using Identity.Domain.Users;

namespace Identity.Domain.UnitTests.Users;

public sealed class RefreshTokenTests
{
    private static readonly DateTime NowUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FamilyId = Guid.NewGuid();
    private static readonly UserId UserId = UserId.New();

    [Fact]
    public void Create_ValidArguments_SetsPropertiesAndIsNotRevoked()
    {
        DateTime expiresOnUtc = NowUtc.AddDays(7);

        var token = RefreshToken.Create("token-hash", UserId, FamilyId, expiresOnUtc, NowUtc);

        Assert.Equal("token-hash", token.TokenHash);
        Assert.Equal(UserId, token.UserId);
        Assert.Equal(FamilyId, token.FamilyId);
        Assert.Equal(expiresOnUtc, token.ExpiresOnUtc);
        Assert.Equal(NowUtc, token.CreatedOnUtc);
        Assert.Null(token.RevokedOnUtc);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void Revoke_ActiveToken_SetsRevokedOnUtc()
    {
        RefreshToken token = CreateToken();

        token.Revoke(NowUtc.AddMinutes(5));

        Assert.True(token.IsRevoked);
        Assert.Equal(NowUtc.AddMinutes(5), token.RevokedOnUtc);
    }

    [Fact]
    public void Revoke_AlreadyRevokedToken_KeepsFirstRevocationTime()
    {
        RefreshToken token = CreateToken();
        DateTime firstRevocation = NowUtc.AddMinutes(5);

        token.Revoke(firstRevocation);
        token.Revoke(firstRevocation.AddHours(1));

        Assert.Equal(firstRevocation, token.RevokedOnUtc);
    }

    [Fact]
    public void IsExpired_BeforeExpiry_ReturnsFalse()
    {
        RefreshToken token = CreateToken();

        Assert.False(token.IsExpired(NowUtc.AddDays(7).AddTicks(-1)));
    }

    [Fact]
    public void IsExpired_AtExpiry_ReturnsTrue()
    {
        RefreshToken token = CreateToken();

        Assert.True(token.IsExpired(NowUtc.AddDays(7)));
    }

    [Fact]
    public void IsExpired_AfterExpiry_ReturnsTrue()
    {
        RefreshToken token = CreateToken();

        Assert.True(token.IsExpired(NowUtc.AddDays(8)));
    }

    private static RefreshToken CreateToken() =>
        RefreshToken.Create("token-hash", UserId, FamilyId, NowUtc.AddDays(7), NowUtc);
}
