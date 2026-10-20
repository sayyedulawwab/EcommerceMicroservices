using Identity.Infrastructure.Auth;

namespace Identity.Application.UnitTests.Infrastructure;

public sealed class SecureTokenServiceTests
{
    private readonly SecureTokenService _service = new();

    [Fact]
    public void Generate_CalledRepeatedly_ReturnsDistinctTokens()
    {
        HashSet<string> tokens = [];

        for (int i = 0; i < 100; i++)
        {
            tokens.Add(_service.Generate());
        }

        Assert.Equal(100, tokens.Count);
    }

    [Fact]
    public void Generate_Always_ReturnsUrlSafeTokenOf256Bits()
    {
        string token = _service.Generate();

        // 32 bytes in unpadded base64url is 43 characters.
        Assert.Equal(43, token.Length);
        Assert.All(token, c => Assert.True(char.IsAsciiLetterOrDigit(c) || c is '-' or '_', $"Unexpected character '{c}'"));
        Assert.Equal(token, Uri.EscapeDataString(token));
    }

    [Fact]
    public void Hash_SameInput_IsDeterministic()
    {
        Assert.Equal(_service.Hash("some-token"), _service.Hash("some-token"));
    }

    [Fact]
    public void Hash_AnyInput_Returns64HexCharacters()
    {
        string hash = _service.Hash(_service.Generate());

        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.True(char.IsAsciiHexDigit(c), $"Unexpected character '{c}'"));
    }

    [Fact]
    public void Hash_DifferentInputs_ReturnsDifferentHashes()
    {
        Assert.NotEqual(_service.Hash("token-a"), _service.Hash("token-b"));
    }

    [Fact]
    public void Hash_KnownInput_MatchesSha256()
    {
        // SHA-256("abc") from FIPS 180-2.
        Assert.Equal(
            "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD",
            _service.Hash("abc"),
            ignoreCase: true);
    }
}
