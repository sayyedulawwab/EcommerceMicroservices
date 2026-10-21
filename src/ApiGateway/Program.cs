using ApiGateway;
using ApiGateway.Extensions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string otlpLogsEndpoint = builder.Configuration["Otlp:LogsEndpoint"] ?? string.Empty;
string otlpTracesEndpoint = builder.Configuration["Otlp:TracesEndpoint"] ?? string.Empty;
string otlpMetricsEndpoint = builder.Configuration["Otlp:MetricsEndpoint"] ?? string.Empty;

// The gateway starts every request's trace; the HttpClient instrumentation covers YARP's forwarded calls,
// so a trace spans gateway -> service.
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
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddServices(builder.Configuration);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
}

app.UseCors(DependencyInjection.FrontendCorsPolicy);

app.MapReverseProxy();

app.MapHealthChecks("/health");

await app.RunAsync();
