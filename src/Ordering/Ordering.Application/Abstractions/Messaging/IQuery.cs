using MediatR;
using SharedKernel.Domain;

namespace Ordering.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
