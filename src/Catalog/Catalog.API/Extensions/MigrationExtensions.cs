using Catalog.Infrastructure;
using Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.EnsureDeleted();

        dbContext.Database.Migrate();
        DataSeeder.SeedData(dbContext);
    }
}