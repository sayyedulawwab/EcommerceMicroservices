using ApiGateway;
using ApiGateway.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
