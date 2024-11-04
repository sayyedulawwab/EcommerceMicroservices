namespace SharedLibrary.Events;
public record OrderStockItem(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);