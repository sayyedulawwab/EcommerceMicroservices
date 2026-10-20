namespace Identity.Domain.Users;

public interface IUserTokenRepository
{
    void Add(UserToken token);
    Task<UserToken?> GetAsync(long userId, UserTokenPurpose purpose, string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Consumes every outstanding token of this purpose, so only the most recently mailed link works.</summary>
    Task ConsumeAllAsync(long userId, UserTokenPurpose purpose, DateTime utcNow, CancellationToken cancellationToken = default);

    Task<int> DeleteStaleAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default);
}
