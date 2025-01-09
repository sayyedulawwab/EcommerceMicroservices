using Identity.API;
using Identity.API.Extensions;
using Identity.Application;
using Identity.Infrastructure;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System.Diagnostics;
using System.Globalization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Logging.ClearProviders();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddSource(builder.Environment.ApplicationName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("NServiceBus.*")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(builder.Environment.ApplicationName)
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService(builder.Environment.ApplicationName));
    options.AddOtlpExporter(x =>
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        x.Endpoint = new Uri("http://seq-logging:5341/ingest/otlp/v1/logs");
#pragma warning restore S1075 // URIs should not be hardcoded
        x.Protocol = OtlpExportProtocol.HttpProtobuf;
    });

});

var endpointConfiguration = new EndpointConfiguration(builder.Environment.ApplicationName);

TransportExtensions<RabbitMQTransport> transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
transport.UseConventionalRoutingTopology(QueueType.Quorum);
transport.ConnectionString("host=rabbitmq-broker;username=guest;password=guest");
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.EnableOpenTelemetry();
endpointConfiguration.Conventions().DefiningEventsAs(t => t.Namespace == "SharedKernel.Events");

endpointConfiguration.EnableInstallers();

builder.UseNServiceBus(endpointConfiguration);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
    app.ApplyMigrations();
}

//app.UseRequestContextLogging();

//app.UseSerilogRequestLogging();

app.UseCustomExceptionHandler();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
