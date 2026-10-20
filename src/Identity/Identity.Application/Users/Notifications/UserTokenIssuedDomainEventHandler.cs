using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Email;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;

namespace Identity.Application.Users.Notifications;

/// <summary>Mails the link for a newly issued token. Runs after the token is saved, so the link always works.</summary>
internal sealed class UserTokenIssuedDomainEventHandler(
    IEmailSender emailSender,
    IOptions<AccountOptions> accountOptions)
    : IDomainEventHandler<UserTokenIssuedDomainEvent>
{
    public async Task Handle(UserTokenIssuedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        string frontendBaseUrl = accountOptions.Value.FrontendBaseUrl.TrimEnd('/');
        string query = $"email={Uri.EscapeDataString(domainEvent.Email)}&token={Uri.EscapeDataString(domainEvent.RawToken)}";

        (string subject, string body) = domainEvent.Purpose switch
        {
            UserTokenPurpose.PasswordReset => (
                "Reset your password",
                $"Hi {domainEvent.FirstName},\n\nUse this link to choose a new password:\n{frontendBaseUrl}/reset-password?{query}\n\n" +
                $"The link expires in {accountOptions.Value.PasswordResetTokenLifetimeMinutes} minutes. If you didn't ask for it, ignore this email."),
            _ => (
                "Confirm your email address",
                $"Hi {domainEvent.FirstName},\n\nConfirm your email address with this link:\n{frontendBaseUrl}/confirm-email?{query}\n\n" +
                $"The link expires in {accountOptions.Value.EmailConfirmationTokenLifetimeHours} hours.")
        };

        await emailSender.SendAsync(domainEvent.Email, subject, body, cancellationToken);
    }
}
