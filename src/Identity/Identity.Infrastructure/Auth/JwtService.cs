using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Auth;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Infrastructure.Auth;

internal sealed class JwtService(IOptions<JwtOptions> jwtOptions, IDateTimeProvider dateTimeProvider) : IJwtService
{
    public AccessToken CreateAccessToken(User user)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ];

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.Admin));
        }

        DateTime expiresOnUtc = dateTimeProvider.UtcNow.AddMinutes(jwtOptions.Value.ExpiresInMinutes);

        var jwt = new JwtSecurityToken(
                    jwtOptions.Value.Issuer,
                    jwtOptions.Value.Audience,
                    claims,
                    null,
                    expiresOnUtc,
                    signingCredentials);

        string token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AccessToken(token, expiresOnUtc);
    }
}
