using NLog.Web;
using Payment.Processor.Application.Abstractions;
using Payment.Processor.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IPaymentService, PaymentService>();

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Host.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("Payment");

    endpointConfiguration.UseTransport<LearningTransport>();
    endpointConfiguration.UsePersistence<LearningPersistence>();
    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    return endpointConfiguration;
});

var app = builder.Build();

app.Run();
