namespace ApiGateway.Extensions;

public static class ApplicationBuilderExtensions
{
    private static readonly string[] Services = ["identity", "catalog", "cart", "ordering"];

    /// <summary>One Swagger UI for every service; each document is proxied from its service (see the *-swagger-route routes).</summary>
    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.UseSwaggerUI(options =>
        {
            foreach (string service in Services)
            {
                options.SwaggerEndpoint($"/swagger/{service}/v1/swagger.json", $"{service} API V1");
            }
        });

        return app;
    }
}
