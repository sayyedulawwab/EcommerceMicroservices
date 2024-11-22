using MediatR;
using SharedKernel.Domain;

namespace Catalog.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}