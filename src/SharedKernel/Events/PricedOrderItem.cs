namespace SharedKernel.Events;

public record PricedOrderItem(Guid ProductId, string ProductName, decimal PriceAmount, string PriceCurrency, int Quantity);
