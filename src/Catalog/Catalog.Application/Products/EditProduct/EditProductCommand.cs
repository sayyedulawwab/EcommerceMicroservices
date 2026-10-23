using SharedKernel.Messaging;

namespace Catalog.Application.Products.EditProduct;

public record EditProductCommand(Guid Id, string Name, string Description, string PriceCurrency, decimal PriceAmount, int Quantity, Guid CategoryId) : ICommand<Guid>;