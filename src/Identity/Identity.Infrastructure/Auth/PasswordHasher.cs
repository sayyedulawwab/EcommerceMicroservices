using Identity.Application.Abstractions.Auth;
using System.Security.Cryptography;

namespace Identity.Infrastructure.Auth;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32; // 256 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 100_000;

    public string GenerateSalt()
    {
        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        return Convert.ToBase64String(salt);
    }

    public string Hash(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        var hashData = new PasswordHashData
        {
            Iterations = Iterations,
            Salt = salt,
            Hash = Convert.ToBase64String(hash)
        };

        return hashData.ToString();
    }

    public bool Verify(string enteredPassword, string storedPasswordHash)
    {
        var hashData = PasswordHashData.Parse(storedPasswordHash);

        byte[] saltBytes = Convert.FromBase64String(hashData.Salt);

        byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
            enteredPassword,
            saltBytes,
            hashData.Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(
            enteredHash,
            Convert.FromBase64String(hashData.Hash));
    }
}