namespace Cart.Application.Carts.Events;
internal record OrderStockItem(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity);