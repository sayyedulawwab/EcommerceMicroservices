﻿using Catalog.Domain.Abstractions;
using MediatR;

namespace Catalog.Application.Abstractions.Messaging;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
