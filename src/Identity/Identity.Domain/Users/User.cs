using Identity.Domain.Abstractions;

namespace Identity.Domain.Users;
public sealed class User : Entity<long>
{
    private User(string firstName, string lastName, string email, string passwordHash, string passwordSalt, bool isAdmin, DateTime createdOn)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        IsAdmin = isAdmin;
        CreatedOn = createdOn;
    }

    private User()
    {
    }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordSalt { get; private set; }
    public bool IsAdmin { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public static User Create(string firstName, string lastName, string email, string passwordHash, string passwordSalt, bool isAdmin, DateTime createdOn)
    {
        var user = new User(firstName, lastName, email, passwordHash, passwordSalt, isAdmin, createdOn);

        return user;
    }
}
