using Asp.Versioning;
using Cart.API;
using Cart.API.Extensions;
using Cart.Application;
using Cart.Application.Carts.RemoveCart;
using Cart.Infrastructure;
using MassTransit;
using MassTransit.Configuration;
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

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();

    busConfigurator.AddConsumer<OrderStatusChangedToPaidIntegrationEventHandler>();

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
            ["service.name"] = "Cart.API",
            ["deployment.environment"] = "Development"
        };
    }), true, writeToProviders: false);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Cart.API"))
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
}

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseCustomExceptionHandler();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
