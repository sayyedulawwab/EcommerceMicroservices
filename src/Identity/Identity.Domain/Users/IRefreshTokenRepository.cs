namespace Identity.Domain.Users;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(long userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> DeleteStaleAsync(DateTime expiredBeforeUtc, DateTime revokedBeforeUtc, CancellationToken cancellationToken = default);
}
