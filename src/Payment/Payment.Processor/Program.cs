using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payment.Processor.Application.Abstractions;
using Payment.Processor.Infrastructure;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Payment.Processor"))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSource("NServiceBus");

        tracing.AddOtlpExporter();
    });

builder.Host.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("Payment");

    TransportExtensions<RabbitMQTransport> transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
    transport.UseConventionalRoutingTopology(QueueType.Quorum);
    transport.ConnectionString("host=rabbitmq-broker;username=guest;password=guest");

    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    endpointConfiguration.Conventions().DefiningEventsAs(t => t.Namespace == "SharedKernel.Events");

    endpointConfiguration.EnableInstallers();


    return endpointConfiguration;
});

WebApplication app = builder.Build();

await app.RunAsync();
