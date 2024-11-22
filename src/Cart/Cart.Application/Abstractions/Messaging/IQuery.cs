using MediatR;
using SharedKernel.Domain;

namespace Cart.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}