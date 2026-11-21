using Cart.Application.Abstractions.Behaviors;
using SharedKernel.Messaging;
using SharedKernel.Domain;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Application;
public static class DependencyInjection
{
    // Every class in this assembly that implements one of these is registered, so a new use case needs no registration.
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IDomainEventHandler<>),
    ];

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // publicOnly: false because the handlers are internal sealed. Scoped: they use the request's DbContext.
        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes.AssignableToAny(HandlerInterfaces), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Each TryDecorate wraps what is registered so far, so the last one is the outermost:
        // logging -> validation -> handler.
        services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidatingCommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidatingCommandHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(ValidatingQueryHandler<,>));

        services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingCommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingCommandHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
