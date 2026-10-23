using SharedKernel.Domain;

namespace Identity.Domain.Users;

/// <summary>
/// One link in a rotation chain. Every refresh revokes the presented token and issues a new one in the same
/// <see cref="FamilyId"/> (one family per login session). Only the SHA-256 hash of the token is stored.
/// </summary>
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    private RefreshToken(RefreshTokenId id, string tokenHash, UserId userId, Guid familyId, DateTime expiresOnUtc, DateTime createdOnUtc)
        : base(id)
    {
        TokenHash = tokenHash;
        UserId = userId;
        FamilyId = familyId;
        ExpiresOnUtc = expiresOnUtc;
        CreatedOnUtc = createdOnUtc;
    }

    public string TokenHash { get; private set; }
    public UserId UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }
    // Required navigation, loaded with Include. EF Core sets it; it is never null after a query.
    public User User { get; } = null!;

    public bool IsRevoked => RevokedOnUtc.HasValue;

    public static RefreshToken Create(string tokenHash, UserId userId, Guid familyId, DateTime expiresOnUtc, DateTime createdOnUtc)
    {
        return new RefreshToken(RefreshTokenId.New(), tokenHash, userId, familyId, expiresOnUtc, createdOnUtc);
    }

    public bool IsExpired(DateTime utcNow) => ExpiresOnUtc <= utcNow;

    public void Revoke(DateTime utcNow)
    {
        RevokedOnUtc ??= utcNow;
    }
}
