using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>
/// Signs access tokens with the services' shared Jwt settings (Audience and Issuer in appsettings.json, SecretKey in appsettings.Development.json), so tests can call Catalog, Cart and
/// Ordering with an Authorization header without going through Identity.
/// </summary>
public static class TestJwt
{
    private const string Issuer = "Ecommerce";
    private const string Audience = "Ecommerce";
    private const string SecretKey = "185C2E54-C194-4FC9-8B0D-B2C25FB577D3";

    public static string Create(Guid userId, bool isAdmin = false)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, $"user{userId}@test.local")
        ];

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            null,
            DateTime.UtcNow.AddMinutes(30),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static Guid NewUserId() => Guid.CreateVersion7();
}
