namespace Catalog.API.Controllers.Products.EditProduct;

public record EditProductRequest(string name, string description, string priceCurrency, decimal priceAmount,
    int quantity, long categoryId);
