using Identity.Application.Abstractions.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Domain;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Infrastructure.Auth;
internal sealed class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    public JwtService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }
    public Result<string> GetAccessToken(string email, long userId, CancellationToken cancellationToken = default)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims = [
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture))
            ];

        var jwt = new JwtSecurityToken(
                    _jwtOptions.Issuer,
                    _jwtOptions.Audience,
                    claims,
                    null,
                    DateTime.UtcNow.AddHours(1),
                    signingCredentials);

        string token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return Result.Success(token);
    }
}
