namespace Identity.Domain.Users;

public record RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New() => new(Guid.CreateVersion7());
}
