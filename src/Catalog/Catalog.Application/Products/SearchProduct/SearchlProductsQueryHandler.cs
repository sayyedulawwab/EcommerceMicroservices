using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using System.Linq.Expressions;

namespace Catalog.Application.Products.SearchProduct;

internal sealed class SearchlProductsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        string? keyword = string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword.Trim();

        CategoryId? categoryId = request.CategoryId is Guid requestedCategoryId ? new CategoryId(requestedCategoryId) : null;

        IQueryable<Product> query = dbContext.Products.AsNoTracking().Where(product =>
            (categoryId == null || product.CategoryId == categoryId) &&
            (!request.MinPrice.HasValue || product.Price.Amount >= request.MinPrice) &&
            (!request.MaxPrice.HasValue || product.Price.Amount <= request.MaxPrice) &&
            (keyword == null || product.Name.Contains(keyword) || product.Description.Contains(keyword)));

        int totalRecords = await query.CountAsync(cancellationToken);


        IOrderedQueryable<Product> orderedQuery = request.SortOrder?.ToUpperInvariant() switch
        {
            "DESC" when !string.IsNullOrEmpty(request.SortColumn) =>
                query.OrderByDescending(GetSortExpression(request.SortColumn)),

            _ when !string.IsNullOrEmpty(request.SortColumn) =>
                query.OrderBy(GetSortExpression(request.SortColumn)),

            _ => query.OrderBy(p => p.CreatedOnUtc)
        };

        List<ProductResponse> productResponse = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductResponse()
            {
                Id = product.Id.Value,
                Name = product.Name,
                Description = product.Description,
                PriceAmount = product.Price.Amount,
                PriceCurrency = product.Price.Currency.Code,
                Quantity = product.Quantity,
                CategoryId = product.CategoryId.Value,
                CreatedOnUtc = product.CreatedOnUtc,
                UpdatedOnUtc = product.UpdatedOnUtc,
            })
            .ToListAsync(cancellationToken);

        var pagedProducts = PagedList<ProductResponse>.Create(productResponse, page, pageSize, totalRecords);

        return pagedProducts;
    }


    private static Expression<Func<Product, object>> GetSortExpression(string sortColumn) =>
    sortColumn.ToUpperInvariant() switch
    {
        "NAME" => p => p.Name,
        "PRICEAMOUNT" => p => p.Price.Amount,
        _ => p => p.CreatedOnUtc, // Default to CreatedOnUtc if invalid
    };
}
