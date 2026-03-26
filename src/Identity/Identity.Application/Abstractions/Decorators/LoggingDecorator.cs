using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Abstractions.Decorators;

internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed command {Command}", commandName);
            }
            else
            {
                logger.LogError("Completed command {Command} with error {Error}", commandName, result.Error);
            }

            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            Result result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed command {Command}", commandName);
            }
            else
            {
                logger.LogError("Completed command {Command} with error {Error}", commandName, result.Error);
            }

            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            string queryName = typeof(TQuery).Name;

            logger.LogInformation("Processing query {Query}", queryName);

            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed query {Query}", queryName);
            }
            else
            {
                logger.LogError("Completed query {Query} with error {Error}", queryName, result.Error);
            }

            return result;
        }
    }
}
