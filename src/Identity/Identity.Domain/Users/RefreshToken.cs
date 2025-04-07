using SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Users;
public sealed class RefreshToken : Entity<long>
{
    private RefreshToken(string token, long userId, DateTime expiresOnUtc)
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    private RefreshToken()
    {
    }
    public string Token { get; private set; }
    public long UserId { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public User User { get; }

    public static RefreshToken Create(string token, long userId, DateTime expiresOnUtc)
    {
        var refreshToken = new RefreshToken(token, userId, expiresOnUtc);

        return refreshToken;
    }

    public static RefreshToken Update(RefreshToken refreshToken, string token, DateTime expiresOnUtc)
    {
        refreshToken.Token = token;
        refreshToken.ExpiresOnUtc = expiresOnUtc;

        return refreshToken;
    }
}