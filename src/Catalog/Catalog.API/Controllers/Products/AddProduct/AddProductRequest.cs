namespace Catalog.API.Controllers.Products.AddProduct;

public record AddProductRequest(string name, string description, string priceCurrency, decimal priceAmount, int quantity, long categoryId);
