using MassTransit;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payment.Processor.Application.Abstractions;
using Payment.Processor.Application.Events;
using Payment.Processor.Extensions;
using Payment.Processor.Infrastructure;
using Payment.Processor.Logging;
using Serilog;
using System.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

builder.Host.UseSerilog((context, loggerConfigruration) => loggerConfigruration.ReadFrom.Configuration(builder.Configuration)
.Enrich.With<ActivityEnricher>(), writeToProviders: true);

string otlpLogsEndpoint = builder.Configuration["Otlp:LogsEndpoint"] ?? string.Empty;
string otlpTracesEndpoint = builder.Configuration["Otlp:TracesEndpoint"] ?? string.Empty;
string otlpMetricsEndpoint = builder.Configuration["Otlp:MetricsEndpoint"] ?? string.Empty;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName).AddAttributes(new Dictionary<string, object>
    {
        ["environment.name"] = builder.Environment.EnvironmentName,
        ["service.name"] = builder.Environment.ApplicationName
    }))
    .WithLogging(logging => logging
            .AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(otlpLogsEndpoint);
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            }))
    .WithTracing(tracing => tracing
            .AddSource(builder.Environment.ApplicationName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(otlpTracesEndpoint);
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            }))
    .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
            {
                exporterOptions.Endpoint = new Uri(otlpMetricsEndpoint);
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 60000; // 60 seconds
            }));

builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddHealthChecks();

builder.Services.AddMassTransit(busConfigurator =>
{
    // Queue names are prefixed with the service name so every service that consumes an event gets its own queue.
    busConfigurator.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("payment", false));

    // No database here, so no transactional outbox: the in-memory outbox at least holds published events back
    // until the consumer has finished, and retries transient failures.
    busConfigurator.AddConfigureEndpointsCallback((context, _, endpointConfigurator) =>
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
        endpointConfigurator.UseInMemoryOutbox(context);
    });

    busConfigurator.AddConsumer<OrderStatusChangedToStockConfirmedIntegrationEventHandler>();

    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(new Uri(builder.Configuration["MessageBroker:Host"] ?? string.Empty), h =>
        {
            h.Username(builder.Configuration["MessageBroker:Username"] ?? string.Empty);
            h.Password(builder.Configuration["MessageBroker:Password"] ?? string.Empty);
        });

        configurator.ConfigureEndpoints(context);
    });
});

WebApplication app = builder.Build();

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.MapHealthChecks("/health");

await app.RunAsync();
