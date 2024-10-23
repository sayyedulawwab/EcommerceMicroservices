using NLog.Web;
using Ordering.API.Extensions;
using Ordering.Application;
using Ordering.Infrastructure;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplication()
                .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Host.UseNLog();


builder.Host.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("Ordering");

    endpointConfiguration.UseTransport<LearningTransport>();
    endpointConfiguration.UsePersistence<LearningPersistence>();

    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    return endpointConfiguration;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();
}

app.UseCustomExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
