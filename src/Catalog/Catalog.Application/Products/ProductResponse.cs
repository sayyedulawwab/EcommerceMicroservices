namespace Catalog.Application.Products;
public sealed class ProductResponse
{
    public long Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }
    public long CategoryId { get; init; }
}