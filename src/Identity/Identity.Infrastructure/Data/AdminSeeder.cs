using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;

namespace Identity.Infrastructure.Data;

public sealed class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    public string? Email { get; set; }
    public string? Password { get; set; }
    public string FirstName { get; set; } = "Admin";
    public string LastName { get; set; } = "User";
}

/// <summary>
/// Creates the admin account from the <c>AdminUser</c> configuration section, or promotes an existing account
/// with that email. Does nothing when the section has no email or password.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        AdminUserOptions options = scope.ServiceProvider.GetRequiredService<IOptions<AdminUserOptions>>().Value;
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AdminSeeder));

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogInformation("No AdminUser configured; skipping admin seeding");

            return;
        }

        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IPasswordHasher passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        DateTime utcNow = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().UtcNow;

        User? user = await userRepository.GetByEmail(options.Email, cancellationToken);

        if (user is null)
        {
            user = User.Create(
                options.FirstName,
                options.LastName,
                options.Email,
                passwordHasher.Hash(options.Password, passwordHasher.GenerateSalt()),
                true,
                utcNow);

            user.ConfirmEmail(utcNow);

            userRepository.Add(user);

            logger.LogInformation("Created admin user {Email}", user.Email);
        }
        else if (!user.IsAdmin)
        {
            user.PromoteToAdmin(utcNow);

            logger.LogInformation("Promoted {Email} to admin", user.Email);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
