using Identity.Application.Abstractions.Auth;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Auth;

internal sealed class SecureTokenService : ISecureTokenService
{
    private const int TokenSizeInBytes = 32; // 256 bits

    public string Generate()
    {
        // URL-safe, so the same token works in a cookie and in an emailed link.
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
    }

    public string Hash(string token)
    {
        // The tokens are random and long, so a fast unsalted hash is enough; a database leak doesn't expose them.
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
