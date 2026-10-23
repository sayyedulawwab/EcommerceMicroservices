namespace Identity.Application.Users.GetProfile;

/// <summary>The frontend can't read the HttpOnly JWT, so this is how it learns who is signed in.</summary>
public sealed class UserProfileResponse
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public bool EmailConfirmed { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public DateTime CreatedOnUtc { get; init; }
}
