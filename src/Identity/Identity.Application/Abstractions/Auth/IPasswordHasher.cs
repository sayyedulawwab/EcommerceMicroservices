namespace Identity.Application.Abstractions.Auth;
public interface IPasswordHasher
{
    string GenerateSalt();
    string Hash(string password, string salt);
    bool Verify(string enteredPassword, string storedPasswordHash);
}
