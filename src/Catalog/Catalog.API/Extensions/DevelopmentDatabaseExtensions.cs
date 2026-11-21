using Catalog.Infrastructure;
using Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Extensions;

internal static class DevelopmentDatabaseExtensions
{
    /// <summary>
    /// Development only: drops the local database, recreates it from the migrations and seeds the sample catalog.
    /// Every restart starts from a clean database. Other environments migrate in the deploy pipeline, never at startup.
    /// </summary>
    public static async Task PrepareDevelopmentDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        DataSeeder.SeedData(dbContext);
    }
}
