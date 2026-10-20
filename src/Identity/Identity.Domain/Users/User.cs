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
    public bool EmailConfirmed { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static User Create(string firstName, string lastName, string email, string passwordHash, bool isAdmin, DateTime createdOnUtc)
    {
        var user = new User(firstName, lastName, NormalizeEmail(email), passwordHash, isAdmin, createdOnUtc);

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

    /// <summary>Emails are stored trimmed and lower-cased so lookups don't depend on how the user typed them.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Email addresses are conventionally stored and displayed in lower case")]
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public void UpdateProfile(string firstName, string lastName, DateTime utcNow)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedOnUtc = utcNow;
    }

    public void ChangePasswordHash(string passwordHash, DateTime utcNow)
    {
        PasswordHash = passwordHash;
        UpdatedOnUtc = utcNow;
    }

    public void ConfirmEmail(DateTime utcNow)
    {
        EmailConfirmed = true;
        UpdatedOnUtc = utcNow;
    }

    public void PromoteToAdmin(DateTime utcNow)
    {
        IsAdmin = true;
        UpdatedOnUtc = utcNow;
    }
}
