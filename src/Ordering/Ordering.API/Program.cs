using Asp.Versioning;
using MassTransit;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Ordering.API;
using Ordering.API.Extensions;
using Ordering.API.Logging;
using Ordering.Application;
using Ordering.Application.Orders.UpdateOrderStatus;
using Ordering.Infrastructure;
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

// Add services to the container.

builder.Services.AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();

    busConfigurator.AddConsumer<OrderItemsStockConfirmedIntegrationEventHandler>();
    busConfigurator.AddConsumer<OrderItemsStockRejectedIntegrationEventHandler>();
    busConfigurator.AddConsumer<OrderPaymentFailedIntegrationEventHandler>();
    busConfigurator.AddConsumer<OrderPaymentSucceededIntegrationEventHandler>();

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

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddMvc() // This is needed for controllers
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
    app.ApplyMigrations();
}

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseCustomExceptionHandler();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
