using Ordering.Domain.Abstractions;
using MediatR;

namespace Ordering.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
