using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Repositories;
internal sealed class RefreshTokenRepository(ApplicationDbContext dbContext) : Repository<RefreshToken>(dbContext), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        RefreshToken? refreshToken = await DbContext.Set<RefreshToken>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);

        return refreshToken;
    }

    public async Task DeleteByUserIdAsync(long userId)
    {
        await DbContext.Set<RefreshToken>()
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
