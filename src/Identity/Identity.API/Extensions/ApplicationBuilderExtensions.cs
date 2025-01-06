using Identity.API.Middleware;
using Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}
