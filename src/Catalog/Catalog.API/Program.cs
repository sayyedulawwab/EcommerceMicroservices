using Catalog.API;
using Catalog.API.Extensions;
using Catalog.Application;
using Catalog.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Host.UseSerilog((context, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Catalog.API"))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSource("NServiceBus");

        tracing.AddOtlpExporter();
    });

builder.Host.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("Catalog");

    TransportExtensions<RabbitMQTransport> transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
    transport.UseConventionalRoutingTopology(QueueType.Quorum);
    transport.ConnectionString("host=rabbitmq-broker;username=guest;password=guest");
    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    endpointConfiguration.Conventions().DefiningEventsAs(t => t.Namespace == "SharedKernel.Events");

    endpointConfiguration.EnableInstallers();

    return endpointConfiguration;
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
