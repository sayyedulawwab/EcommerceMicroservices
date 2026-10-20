using Microsoft.OpenApi;

namespace Catalog.API.Extensions;

public static class ApplicationBuilderExtensions
{
    private const string ForwardedPrefixHeaderName = "X-Forwarded-Prefix";

    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;

            // Served through the gateway (which sets X-Forwarded-Prefix to e.g. /api/catalog), publish the gateway's
            // paths (/api/catalog/v1/...) instead of this service's own (/api/v1/...).
            options.PreSerializeFilters.Add((document, request) =>
            {
                string? prefix = request.Headers[ForwardedPrefixHeaderName].FirstOrDefault();

                if (string.IsNullOrEmpty(prefix))
                {
                    return;
                }

                var gatewayPaths = new OpenApiPaths();

                foreach ((string path, IOpenApiPathItem pathItem) in document.Paths)
                {
                    gatewayPaths.Add(path.StartsWith("/api/", StringComparison.Ordinal) ? prefix + path["/api".Length..] : path, pathItem);
                }

                document.Paths = gatewayPaths;
            });
        });
        app.MapSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "Catalog API V1"));

        return app;
    }
}
