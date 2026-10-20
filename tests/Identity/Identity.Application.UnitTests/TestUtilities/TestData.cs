using Identity.Domain.Users;

namespace Identity.Application.UnitTests.TestUtilities;

internal static class TestData
{
    public const long UserId = 7;
    public const string Email = "jane@example.com";
    public const string StoredHash = "stored-hash";
    public const string AccessTokenValue = "access-token";

    public static readonly DateTime UtcNow = new(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);

    public static User CreateUser(bool emailConfirmed = false)
    {
        User user = User.Create("Jane", "Doe", Email, StoredHash, isAdmin: false, UtcNow.AddDays(-30)).WithId(UserId);

        if (emailConfirmed)
        {
            user.ConfirmEmail(UtcNow.AddDays(-29));
        }

        return user;
    }
}
