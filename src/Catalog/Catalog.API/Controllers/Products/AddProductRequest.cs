namespace Catalog.API.Controllers.Products;

public record AddProductRequest(string name, string description, string priceCurrency, decimal priceAmount, int quantity, long categoryId);
