using SharedKernel.Messaging;
using SharedKernel.Domain;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Abstractions.Behaviors;

// Outermost wrapper: logs when a command or query starts and how it ended, including
// requests the validating wrapper rejected.

internal sealed class LoggingCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<LoggingCommandHandler<TCommand>> logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        string name = typeof(TCommand).Name;
        HandlerLog.CommandStarted(logger, name);

        Result result = await inner.HandleAsync(command, cancellationToken);

        HandlerLog.CommandEnded(logger, name, result);
        return result;
    }
}

internal sealed class LoggingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<LoggingCommandHandler<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        string name = typeof(TCommand).Name;
        HandlerLog.CommandStarted(logger, name);

        Result<TResponse> result = await inner.HandleAsync(command, cancellationToken);

        HandlerLog.CommandEnded(logger, name, result);
        return result;
    }
}

internal sealed class LoggingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    ILogger<LoggingQueryHandler<TQuery, TResponse>> logger)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        string name = typeof(TQuery).Name;
        HandlerLog.QueryStarted(logger, name);

        Result<TResponse> result = await inner.HandleAsync(query, cancellationToken);

        HandlerLog.QueryEnded(logger, name, result);
        return result;
    }
}

// Source-generated log methods: the message templates are parsed once at compile time,
// and nothing is formatted when the level is turned off.
internal static partial class HandlerLog
{
    public static void CommandEnded(ILogger logger, string command, Result result)
    {
        if (result.IsSuccess)
        {
            CommandSucceeded(logger, command);
        }
        else
        {
            CommandFailed(logger, command, result.Error.Name, result.Error.Description);
        }
    }

    public static void QueryEnded(ILogger logger, string query, Result result)
    {
        if (result.IsSuccess)
        {
            QuerySucceeded(logger, query);
        }
        else
        {
            QueryFailed(logger, query, result.Error.Name, result.Error.Description);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing command {Command}")]
    public static partial void CommandStarted(ILogger logger, string command);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed command {Command}")]
    private static partial void CommandSucceeded(ILogger logger, string command);

    [LoggerMessage(Level = LogLevel.Error, Message = "Completed command {Command} with error {ErrorCode}: {ErrorDescription}")]
    private static partial void CommandFailed(ILogger logger, string command, string errorCode, string errorDescription);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing query {Query}")]
    public static partial void QueryStarted(ILogger logger, string query);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed query {Query}")]
    private static partial void QuerySucceeded(ILogger logger, string query);

    [LoggerMessage(Level = LogLevel.Error, Message = "Completed query {Query} with error {ErrorCode}: {ErrorDescription}")]
    private static partial void QueryFailed(ILogger logger, string query, string errorCode, string errorDescription);
}
