namespace Catalog.API.Controllers.Products;

public record SearchProductRequest(Guid? CategoryId,
                                    decimal? MinPrice,
                                    decimal? MaxPrice,
                                    string? Keyword,
                                    string? SortColumn,
                                    string? SortOrder,
                                    int Page = 1,
                                    int PageSize = 10);
