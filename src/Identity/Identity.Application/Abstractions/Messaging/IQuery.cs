using MediatR;
using SharedKernel.Domain;

namespace Identity.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}