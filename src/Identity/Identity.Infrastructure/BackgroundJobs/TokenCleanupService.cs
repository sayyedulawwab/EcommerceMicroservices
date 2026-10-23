using Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.BackgroundJobs;

public sealed class TokenCleanupOptions
{
    public const string SectionName = "TokenCleanup";

    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Revoked refresh tokens are kept this long so a replayed token is still recognised as reuse.</summary>
    public int RevokedRefreshTokenRetentionDays { get; set; } = 7;

    public int UsedOrExpiredUserTokenRetentionDays { get; set; } = 1;
}

/// <summary>Periodically deletes expired and long-revoked refresh tokens and spent email tokens.</summary>
internal sealed class TokenCleanupService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TokenCleanupOptions> options,
    TimeProvider timeProvider,
    ILogger<TokenCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.IntervalMinutes), timeProvider);

        do
        {
            try
            {
                await CleanUpAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failed run (e.g. the database is still starting) is retried on the next tick.
                logger.LogWarning(exception, "Token cleanup failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanUpAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        DateTime revokedBeforeUtc = utcNow.AddDays(-options.Value.RevokedRefreshTokenRetentionDays);
        DateTime userTokensOlderThanUtc = utcNow.AddDays(-options.Value.UsedOrExpiredUserTokenRetentionDays);

        int refreshTokens = await dbContext.RefreshTokens
            .Where(r => r.ExpiresOnUtc < utcNow || r.RevokedOnUtc < revokedBeforeUtc)
            .ExecuteDeleteAsync(cancellationToken);

        int userTokens = await dbContext.UserTokens
            .Where(t => t.ExpiresOnUtc < userTokensOlderThanUtc || t.ConsumedOnUtc < userTokensOlderThanUtc)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Token cleanup deleted {RefreshTokens} refresh tokens and {UserTokens} email tokens", refreshTokens, userTokens);
    }
}
