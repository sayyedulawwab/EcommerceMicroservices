namespace Catalog.Application.Products;
public sealed class ProductResponse
{
    public long Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public long CategoryId { get; init; }
}