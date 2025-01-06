namespace Catalog.API.Controllers.Products.SearchProduct;

public record SearchProductRequest(long? CategoryId,
                                    decimal? MinPrice,
                                    decimal? MaxPrice,
                                    string? Keyword,
                                    string? SortColumn,
                                    string? SortOrder,
                                    int Page = 1,
                                    int PageSize = 10);
