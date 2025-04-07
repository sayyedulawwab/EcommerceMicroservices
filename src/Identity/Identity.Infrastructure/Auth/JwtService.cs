using Identity.Application.Abstractions.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Domain;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Auth;
internal sealed class JwtService(IOptions<JwtOptions> jwtOptions) : IJwtService
{
    public string GetAccessToken(string email, long userId, CancellationToken cancellationToken = default)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims = [
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture))
            ];

        var jwt = new JwtSecurityToken(
                    jwtOptions.Value.Issuer,
                    jwtOptions.Value.Audience,
                    claims,
                    null,
                    DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpiresInMinutes),
                    signingCredentials);

        string token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return token;
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
