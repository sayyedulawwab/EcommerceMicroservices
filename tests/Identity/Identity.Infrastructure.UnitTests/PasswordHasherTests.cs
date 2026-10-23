using Identity.Infrastructure.Auth;

namespace Identity.Infrastructure.UnitTests;

public sealed class PasswordHasherTests
{
    private const string Credential = "Correct-Horse-1";

    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_SamePassword_ReturnsTrue()
    {
        string stored = _hasher.Hash(Credential, _hasher.GenerateSalt());

        Assert.True(_hasher.Verify(Credential, stored));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        string stored = _hasher.Hash(Credential, _hasher.GenerateSalt());

        Assert.False(_hasher.Verify("Wrong-Horse-1", stored));
    }

    [Fact]
    public void Hash_SamePasswordDifferentSalts_ProducesDifferentHashes()
    {
        string first = _hasher.Hash(Credential, _hasher.GenerateSalt());
        string second = _hasher.Hash(Credential, _hasher.GenerateSalt());

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify(Credential, first));
        Assert.True(_hasher.Verify(Credential, second));
    }

    [Fact]
    public void Hash_SamePasswordSameSalt_IsDeterministic()
    {
        string salt = _hasher.GenerateSalt();

        Assert.Equal(_hasher.Hash(Credential, salt), _hasher.Hash(Credential, salt));
    }

    [Fact]
    public void Hash_Always_DoesNotContainPlainPassword()
    {
        string stored = _hasher.Hash(Credential, _hasher.GenerateSalt());

        Assert.DoesNotContain(Credential, stored, StringComparison.Ordinal);
    }
}
