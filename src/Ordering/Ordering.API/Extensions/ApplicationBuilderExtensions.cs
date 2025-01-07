using Microsoft.EntityFrameworkCore;
using Ordering.API.Middleware;
using Ordering.Infrastructure;

namespace Ordering.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}