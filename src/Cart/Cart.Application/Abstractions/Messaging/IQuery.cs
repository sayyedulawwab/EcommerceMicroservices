using Cart.Domain.Abstractions;
using MediatR;

namespace Cart.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
