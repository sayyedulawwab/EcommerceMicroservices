﻿namespace Catalog.API.Controllers.Products.SearchProduct;

public record SearchProductRequest(long? categoryId,
                                    decimal? minPrice,
                                    decimal? maxPrice,
                                    string keyword,
                                    string? sortColumn,
                                    string? sortOrder,
                                    int page = 1,
                                    int pageSize = 10);
