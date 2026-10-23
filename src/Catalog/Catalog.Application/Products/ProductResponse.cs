namespace Catalog.Application.Products;

public sealed class ProductResponse
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal PriceAmount { get; init; }
    public required string PriceCurrency { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public long CategoryId { get; init; }
}