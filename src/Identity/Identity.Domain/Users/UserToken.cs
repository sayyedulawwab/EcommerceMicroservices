using SharedKernel.Domain;

namespace Identity.Domain.Users;

/// <summary>
/// A single-use token mailed to the user (email confirmation, password reset). Only its hash is stored; the raw
/// value travels in <see cref="UserTokenIssuedDomainEvent"/> to the handler that sends the email, after the save.
/// </summary>
public sealed class UserToken : Entity<UserTokenId>
{
    private UserToken(UserTokenId id, User user, UserTokenPurpose purpose, string tokenHash, DateTime expiresOnUtc, DateTime createdOnUtc)
        : base(id)
    {
        // Set through the navigation so EF fills in UserId even when the user is being created in the same save.
        User = user;
        UserId = user.Id;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresOnUtc = expiresOnUtc;
        CreatedOnUtc = createdOnUtc;
    }

    #pragma warning disable CS8618 // Used by EF Core, which can't bind the User navigation through the constructor
    private UserToken()
    {
    }
    #pragma warning restore CS8618

    public UserId UserId { get; private set; }
    public UserTokenPurpose Purpose { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ConsumedOnUtc { get; private set; }
    public User User { get; private set; }

    public static UserToken Issue(
        User user,
        UserTokenPurpose purpose,
        string rawToken,
        string tokenHash,
        DateTime expiresOnUtc,
        DateTime createdOnUtc)
    {
        var token = new UserToken(UserTokenId.New(), user, purpose, tokenHash, expiresOnUtc, createdOnUtc);

        token.RaiseDomainEvent(new UserTokenIssuedDomainEvent(user.Email, user.FirstName, purpose, rawToken));

        return token;
    }

    public bool IsUsable(DateTime utcNow) => ConsumedOnUtc is null && ExpiresOnUtc > utcNow;

    public void Consume(DateTime utcNow)
    {
        ConsumedOnUtc ??= utcNow;
    }
}
