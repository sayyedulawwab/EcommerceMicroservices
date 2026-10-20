using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

internal sealed class UserRepository(ApplicationDbContext dbContext) : Repository<User>(dbContext), IUserRepository
{
    public async Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = User.NormalizeEmail(email);

        User? user = await DbContext.Set<User>()
            .Where(x => x.Email == normalizedEmail)
            .FirstOrDefaultAsync(cancellationToken);

        return user;

    }

}
