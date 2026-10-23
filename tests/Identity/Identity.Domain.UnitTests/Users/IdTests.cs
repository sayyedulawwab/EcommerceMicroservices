using Identity.Domain.Users;

namespace Identity.Domain.UnitTests.Users;

public sealed class IdTests
{
    [Fact]
    public void UserId_New_CreatesVersion7Guid()
    {
        Assert.Equal(7, UserId.New().Value.Version);
    }

    [Fact]
    public void RefreshTokenId_New_CreatesVersion7Guid()
    {
        Assert.Equal(7, RefreshTokenId.New().Value.Version);
    }

    [Fact]
    public void UserTokenId_New_CreatesVersion7Guid()
    {
        Assert.Equal(7, UserTokenId.New().Value.Version);
    }
}
