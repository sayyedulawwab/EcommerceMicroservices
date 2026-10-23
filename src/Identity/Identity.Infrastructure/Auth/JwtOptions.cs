namespace Identity.Infrastructure.Auth;

public sealed class JwtOptions
{
    public required string Issuer { get; set; }
    public required string Audience { get; init; }
    public required string SecretKey { get; set; }
    public int ExpiresInMinutes { get; set; }
}
