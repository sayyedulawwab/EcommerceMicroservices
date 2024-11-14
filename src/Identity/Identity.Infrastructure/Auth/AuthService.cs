using Identity.Application.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Auth;
internal sealed class AuthService : IAuthService
{
    public string GenerateSalt()
    {
        byte[] randomBytes = new byte[128 / 8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashPassword(string password, string salt)
    {
        SHA256 sha256Hash = SHA256.Create();
        return Convert.ToBase64String(sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password + salt)));
    }
}
