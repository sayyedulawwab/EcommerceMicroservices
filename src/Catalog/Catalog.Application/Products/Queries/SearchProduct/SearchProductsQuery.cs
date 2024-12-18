using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Products.Queries.SearchProduct;
public record SearchProductsQuery(
    long? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    string keyword,
    int page,
    int pageSize,
    string? sortColumn,
    string? sortOrder) : IQuery<PagedList<ProductResponse>>;