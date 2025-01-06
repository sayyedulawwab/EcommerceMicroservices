using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.AddProduct;
public record AddProductCommand(string Name, string Description, string PriceCurrency, decimal PriceAmount, int Quantity, long CategoryId) : ICommand<long>;