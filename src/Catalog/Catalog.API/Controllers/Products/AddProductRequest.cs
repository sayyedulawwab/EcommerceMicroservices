using System.Text.Json.Serialization;

namespace Catalog.API.Controllers.Products;

public record AddProductRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string PriceCurrency { get; init; }
    [JsonRequired] public decimal PriceAmount { get; init; }
    [JsonRequired] public int Quantity { get; init; }
    [JsonRequired] public long CategoryId { get; init; }
}

