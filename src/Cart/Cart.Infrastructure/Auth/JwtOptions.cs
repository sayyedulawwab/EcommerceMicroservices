namespace Cart.Infrastructure.Auth;
public sealed class JwtOptions
{
    public string Issuer { get; set; }
    public string Audience { get; init; }
    public string SecretKey { get; set; }
}