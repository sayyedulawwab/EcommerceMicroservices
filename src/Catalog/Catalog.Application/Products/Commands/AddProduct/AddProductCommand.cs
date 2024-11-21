using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.Commands.AddProduct;
public record AddProductCommand(string name, string description, string priceCurrency, decimal priceAmount, int quantity, long categoryId) : ICommand<long>;