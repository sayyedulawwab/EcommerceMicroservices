using SharedKernel.Domain;

namespace Identity.Domain.Users;
public sealed class User : Entity<long>
{
    private User(string firstName, string lastName, string email, string passwordHash, bool isAdmin, DateTime createdOnUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        IsAdmin = isAdmin;
        CreatedOnUtc = createdOnUtc;
    }

    private User()
    {
    }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsAdmin { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static User Create(string firstName, string lastName, string email, string passwordHash, bool isAdmin, DateTime createdOnUtc)
    {
        var user = new User(firstName, lastName, email, passwordHash, isAdmin, createdOnUtc);

        return user;
    }

    public static User Update(User user, string firstName, string lastName, bool isAdmin, DateTime updatedOnUtc)
    {
        user.FirstName = firstName;
        user.LastName = lastName;
        user.IsAdmin = isAdmin;
        user.UpdatedOnUtc = updatedOnUtc;

        return user;
    }
}
