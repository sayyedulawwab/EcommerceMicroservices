using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(long id) : IQuery<ProductResponse>;