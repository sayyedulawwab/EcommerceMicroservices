using MassTransit;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payment.Processor.Application.Abstractions;
using Payment.Processor.Application.Events;
using Payment.Processor.Extensions;
using Payment.Processor.Infrastructure;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System.Diagnostics;
using System.Globalization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();

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

builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, loggerConfigruration) => loggerConfigruration.ReadFrom.Configuration(builder.Configuration)
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://seq-logging:5341/ingest/otlp/v1/logs";
        options.Protocol = OtlpProtocol.HttpProtobuf;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "Payment.Processor",
            ["deployment.environment"] = "Development"
        };
    }), true, writeToProviders: false);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Payment.Processor"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
        .AddOtlpExporter());

//builder.Logging.AddOpenTelemetry(logging =>
//{
//    logging.IncludeScopes = true;
//    logging.IncludeFormattedMessage = true;
//    logging.AddOtlpExporter();
//});

WebApplication app = builder.Build();

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

await app.RunAsync();
