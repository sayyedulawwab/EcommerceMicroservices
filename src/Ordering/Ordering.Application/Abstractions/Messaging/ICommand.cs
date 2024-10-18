using Ordering.Domain.Abstractions;
using MediatR;

namespace Ordering.Application.Abstractions.Messaging;
public interface ICommand : IRequest<Result>, IBaseCommand
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

public interface IBaseCommand
{
}