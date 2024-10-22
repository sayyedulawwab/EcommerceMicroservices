namespace Ordering.Application.Orders.PlaceOrder;
internal class OrderStockItem
{
    private OrderStockItem(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        PriceAmount = priceAmount;
        PriceCurrency = priceCurrency;
        Quantity = quantity;
    }

    public long ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal PriceAmount { get; private set; }
    public string PriceCurrency { get; private set; }
    public int Quantity { get; set; }

    public static OrderStockItem Create(long productId, string productName, decimal priceAmount, string priceCurrency, int quantity)
    {
        var orderStockItem = new OrderStockItem(productId, productName, priceAmount,priceCurrency, quantity);

        return orderStockItem;
    }
}
