namespace Identity.Domain.Users;

public record UserTokenId(Guid Value)
{
    public static UserTokenId New() => new(Guid.CreateVersion7());
}
