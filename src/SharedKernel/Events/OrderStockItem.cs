namespace SharedKernel.Events;
public record OrderStockItem(long ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);