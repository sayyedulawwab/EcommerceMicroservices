using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.SearchProduct;
public record SearchProductsQuery(
    long? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Keyword,
    int Page,
    int PageSize,
    string? SortColumn,
    string? SortOrder) : IQuery<PagedList<ProductResponse>>;