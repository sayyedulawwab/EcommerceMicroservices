using Identity.Application.Abstractions.Email;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts one service in-process against the shared test containers. Runs in the Development environment, so the
/// service migrates (and, for Catalog, seeds) its database on startup exactly like it does under docker compose.
/// </summary>
public sealed class ServiceFactory<TProgram>(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        foreach ((string key, string? value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        // Identity mails links through IEmailSender; tests read them from memory instead of the log.
        builder.ConfigureTestServices(services =>
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(IEmailSender)))
            {
                services.AddSingleton<IEmailSender, CapturingEmailSender>();
            }
        });

        builder.ConfigureTestServices(services =>
            // Don't let a test publish before every consumer queue is bound, or the first messages are dropped.
            services.Configure<MassTransitHostOptions>(options =>
            {
                options.WaitUntilStarted = true;
                options.StartTimeout = TimeSpan.FromSeconds(60);
            }));
    }
}
