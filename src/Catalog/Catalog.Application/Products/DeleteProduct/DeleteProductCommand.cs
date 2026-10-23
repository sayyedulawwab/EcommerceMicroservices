using SharedKernel.Messaging;

namespace Catalog.Application.Products.DeleteProduct;

public record DeleteProductCommand(Guid Id) : ICommand<Guid>;