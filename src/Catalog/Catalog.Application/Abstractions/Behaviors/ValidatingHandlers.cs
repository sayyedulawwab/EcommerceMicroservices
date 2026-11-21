using SharedKernel.Messaging;
using SharedKernel.Domain;
using SharedKernel.Exceptions;
using FluentValidation;

namespace Catalog.Application.Abstractions.Behaviors;

// Each handler interface is a different generic type, so each needs its own validating wrapper.
// All three run the request's validators first and only call the inner handler when none failed.

internal sealed class ValidatingCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        ValidationError? error = await RequestValidation.ValidateAsync(command, validators, cancellationToken);

        return error is null
            ? await inner.HandleAsync(command, cancellationToken)
            : Result.Failure(error);
    }
}

internal sealed class ValidatingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        ValidationError? error = await RequestValidation.ValidateAsync(command, validators, cancellationToken);

        return error is null
            ? await inner.HandleAsync(command, cancellationToken)
            : Result.Failure<TResponse>(error);
    }
}

internal sealed class ValidatingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        ValidationError? error = await RequestValidation.ValidateAsync(query, validators, cancellationToken);

        return error is null
            ? await inner.HandleAsync(query, cancellationToken)
            : Result.Failure<TResponse>(error);
    }
}

internal static class RequestValidation
{
    // The validators run one after another, not in parallel: an async rule may use the request's
    // scoped DbContext, which can't run two operations at the same time.
    public static async Task<ValidationError?> ValidateAsync<TRequest>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        foreach (IValidator<TRequest> validator in validators)
        {
            FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(request, cancellationToken);

            // The field name is the error's name, so the client sees which field is wrong.
            errors.AddRange(result.Errors.Select(failure => Error.BadRequest(failure.PropertyName, failure.ErrorMessage)));
        }

        return errors.Count == 0 ? null : new ValidationError(errors);
    }
}
