using System.Text.Json.Serialization;

namespace Catalog.API.Controllers.Products.EditProduct;

public record EditProductRequest
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string PriceCurrency { get; init; }
    [JsonRequired] public decimal PriceAmount { get; init; }
    [JsonRequired] public int Quantity { get; init; }
    [JsonRequired] public long CategoryId { get; init; }
}