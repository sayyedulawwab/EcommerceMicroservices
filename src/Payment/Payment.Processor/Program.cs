using NLog.Web;
using Payment.Processor.Application.Abstractions;
using Payment.Processor.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IPaymentService, PaymentService>();

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Host.UseNLog();

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
