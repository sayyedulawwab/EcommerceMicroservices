using Identity.Domain.Abstractions;
using MediatR;

namespace Identity.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
