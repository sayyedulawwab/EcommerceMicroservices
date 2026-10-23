using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace Identity.Application.Users;

internal sealed class UserTokenIssuer(
    ISecureTokenService secureTokenService,
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IOptions<AccountOptions> accountOptions)
{
    /// <summary>
    /// Adds a new single-use token for <paramref name="user"/>; the email with the link goes out after the caller saves.
    /// </summary>
    public void Issue(User user, UserTokenPurpose purpose)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        DateTime expiresOnUtc = purpose == UserTokenPurpose.PasswordReset
            ? utcNow.AddMinutes(accountOptions.Value.PasswordResetTokenLifetimeMinutes)
            : utcNow.AddHours(accountOptions.Value.EmailConfirmationTokenLifetimeHours);

        string rawToken = secureTokenService.Generate();

        dbContext.UserTokens.Add(UserToken.Issue(user, purpose, rawToken, secureTokenService.Hash(rawToken), expiresOnUtc, utcNow));
    }
}
