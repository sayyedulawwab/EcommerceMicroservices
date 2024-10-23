namespace Catalog.Application.Products.Events;
internal record OrderStockItem(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);

