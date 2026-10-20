namespace Identity.Application.Abstractions.Auth;

/// <summary>Opaque random tokens (refresh tokens, emailed links) and the hashes they are stored as.</summary>
public interface ISecureTokenService
{
    string Generate();
    string Hash(string token);
}
