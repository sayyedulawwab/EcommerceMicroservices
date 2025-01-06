using System.Globalization;

namespace Identity.Infrastructure.Auth;
public class PasswordHashData
{
    public int Iterations { get; set; }
    public string Salt { get; set; }
    public string Hash { get; set; }

    public override string ToString()
    {
        // Format as Iterations:Salt:Hash
        return $"{Iterations}:{Salt}:{Hash}";
    }

    public static PasswordHashData Parse(string storedPasswordHash)
    {
        // Split format Iterations:Salt:Hash and populate properties
        string[] parts = storedPasswordHash.Split(':');
        return new PasswordHashData
        {
            Iterations = int.Parse(parts[0], CultureInfo.InvariantCulture),
            Salt = parts[1],
            Hash = parts[2]
        };
    }
}
