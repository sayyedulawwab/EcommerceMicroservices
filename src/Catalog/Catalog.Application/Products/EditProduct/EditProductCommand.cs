using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.EditProduct;
public record EditProductCommand(long Id, string Name, string Description, string PriceCurrency, decimal PriceAmount, int Quantity, long CategoryId) : ICommand<long>;