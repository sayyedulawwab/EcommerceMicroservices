var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("Payment");

    endpointConfiguration.UseTransport<LearningTransport>();
    endpointConfiguration.UsePersistence<LearningPersistence>();

    return endpointConfiguration;
});

var app = builder.Build();

app.Run();
