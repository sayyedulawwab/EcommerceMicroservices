using Microsoft.OpenApi;

namespace Cart.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
        app.MapSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "Cart API V1"));

        return app;
    }
}