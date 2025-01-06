using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.GetProductById;
public record GetProductByIdQuery(long Id) : IQuery<ProductResponse>;