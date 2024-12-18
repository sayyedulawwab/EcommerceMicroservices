using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;
internal sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<User?> GetByEmail(string email)
    {
        var user = await DbContext.Set<User>()
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync();

        return user;

    }

}
