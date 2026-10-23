using SharedKernel.Messaging;

namespace Catalog.Application.Products.GetProductById;

public record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;