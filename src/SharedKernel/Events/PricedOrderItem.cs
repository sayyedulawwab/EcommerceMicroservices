namespace SharedKernel.Events;

public record PricedOrderItem(long ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);
