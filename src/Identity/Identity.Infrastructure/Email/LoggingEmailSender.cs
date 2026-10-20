using Identity.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Email;

/// <summary>
/// Stand-in for a real email provider: writes the email (including its link) to the log.
/// Replace the <see cref="IEmailSender"/> registration with an SMTP or API-based sender to send real mail.
/// </summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email to {To} with subject {Subject}:\n{Body}", to, subject, body);

        return Task.CompletedTask;
    }
}
