using Identity.Domain.Users;

namespace Identity.Domain.UnitTests.Users;

public sealed class UserTests
{
    private const string StoredHash = "stored-hash";
    private static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaterUtc = CreatedOnUtc.AddHours(1);

    [Fact]
    public void Create_ValidArguments_SetsProperties()
    {
        var user = User.Create("Jane", "Doe", "jane@example.com", StoredHash, isAdmin: false, CreatedOnUtc);

        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal(StoredHash, user.PasswordHash);
        Assert.False(user.IsAdmin);
        Assert.False(user.EmailConfirmed);
        Assert.Equal(CreatedOnUtc, user.CreatedOnUtc);
        Assert.Null(user.UpdatedOnUtc);
    }

    [Theory]
    [InlineData("  Jane@Example.COM ", "jane@example.com")]
    [InlineData("JOHN@EXAMPLE.COM", "john@example.com")]
    [InlineData("already@lower.com", "already@lower.com")]
    public void Create_EmailWithWhitespaceAndUpperCase_NormalizesEmail(string email, string expected)
    {
        var user = User.Create("Jane", "Doe", email, StoredHash, isAdmin: false, CreatedOnUtc);

        Assert.Equal(expected, user.Email);
    }

    [Fact]
    public void NormalizeEmail_MixedCaseWithWhitespace_ReturnsTrimmedLowerCase()
    {
        string normalized = User.NormalizeEmail("\tMiXeD@Example.Org  ");

        Assert.Equal("mixed@example.org", normalized);
    }

    [Fact]
    public void UpdateProfile_NewNames_ChangesNamesAndUpdatedOn()
    {
        User user = CreateUser();

        user.UpdateProfile("Janet", "Smith", LaterUtc);

        Assert.Equal("Janet", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        Assert.Equal(LaterUtc, user.UpdatedOnUtc);
        Assert.Equal("jane@example.com", user.Email);
    }

    [Fact]
    public void ChangePasswordHash_NewHash_ReplacesHashAndSetsUpdatedOn()
    {
        User user = CreateUser();

        user.ChangePasswordHash("replacement-hash", LaterUtc);

        Assert.Equal("replacement-hash", user.PasswordHash);
        Assert.Equal(LaterUtc, user.UpdatedOnUtc);
    }

    [Fact]
    public void ConfirmEmail_UnconfirmedUser_MarksEmailConfirmed()
    {
        User user = CreateUser();

        user.ConfirmEmail(LaterUtc);

        Assert.True(user.EmailConfirmed);
        Assert.Equal(LaterUtc, user.UpdatedOnUtc);
    }

    [Fact]
    public void PromoteToAdmin_RegularUser_BecomesAdmin()
    {
        User user = CreateUser();

        user.PromoteToAdmin(LaterUtc);

        Assert.True(user.IsAdmin);
        Assert.Equal(LaterUtc, user.UpdatedOnUtc);
    }

    [Fact]
    public void Create_NewUser_RaisesNoDomainEvents()
    {
        User user = CreateUser();

        Assert.Empty(user.GetDomainEvents());
    }

    private static User CreateUser() =>
        User.Create("Jane", "Doe", "jane@example.com", StoredHash, isAdmin: false, CreatedOnUtc);
}
