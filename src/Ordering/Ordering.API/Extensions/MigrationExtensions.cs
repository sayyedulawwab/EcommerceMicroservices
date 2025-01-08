using Microsoft.EntityFrameworkCore;
using Ordering.Infrastructure;

namespace Ordering.API.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.EnsureDeleted();

        dbContext.Database.Migrate();
    }
}