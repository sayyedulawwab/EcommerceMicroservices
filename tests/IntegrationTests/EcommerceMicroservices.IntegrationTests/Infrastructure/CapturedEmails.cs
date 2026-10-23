using Identity.Application.Abstractions.Email;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

public sealed record CapturedEmail(string To, string Subject, string Body)
{
    /// <summary>The raw token in the link of a confirmation or password reset email.</summary>
    public string Token
    {
        get
        {
            Match match = Regex.Match(Body, @"[?&]token=([^&\s]+)", RegexOptions.None, TimeSpan.FromSeconds(1));

            Assert.True(match.Success, $"No token link in email body: {Body}");

            return Uri.UnescapeDataString(match.Groups[1].Value);
        }
    }
}

/// <summary>
/// Test-only stand-in for Identity's email sender (registered by <see cref="ServiceFactory{TProgram}"/>): keeps every
/// mail in memory so a test can read the link a real user would click. Addresses are unique per test, so one shared
/// list is enough.
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private static readonly ConcurrentQueue<CapturedEmail> Sent = new();

    public static IReadOnlyList<CapturedEmail> To(string email) =>
        [.. Sent.Where(mail => string.Equals(mail.To, email, StringComparison.OrdinalIgnoreCase))];

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Enqueue(new CapturedEmail(to, subject, body));

        return Task.CompletedTask;
    }
}
