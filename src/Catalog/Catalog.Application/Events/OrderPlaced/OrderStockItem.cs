namespace Catalog.Application.Events.OrderPlaced;
internal record OrderStockItem(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);

