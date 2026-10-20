using Catalog.Domain.Products;
using SharedKernel.Domain;
using SharedKernel.Messaging;
using System.Linq.Expressions;

namespace Catalog.Application.Products.SearchProduct;

internal sealed class SearchlProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        string? keyword = string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword.Trim();

        Expression<Func<Product, bool>>? filter = product =>
            (!request.CategoryId.HasValue || product.CategoryId == request.CategoryId) &&
            (!request.MinPrice.HasValue || product.Price.Amount >= request.MinPrice) &&
            (!request.MaxPrice.HasValue || product.Price.Amount <= request.MaxPrice) &&
            (keyword == null || product.Name.Contains(keyword) || product.Description.Contains(keyword));


        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = request.SortOrder?.ToUpperInvariant() switch
        {
            "DESC" when !string.IsNullOrEmpty(request.SortColumn) =>
                query => query.OrderByDescending(GetSortExpression(request.SortColumn)),

            _ when !string.IsNullOrEmpty(request.SortColumn) =>
                query => query.OrderBy(GetSortExpression(request.SortColumn)),

            _ => query => query.OrderBy(p => p.CreatedOnUtc)
        };


        (IReadOnlyList<Product> products, int totalRecords) = await productRepository.FindAsync(
            filter, orderBy, page, pageSize, cancellationToken);

        var productResponse = products.Select(product => new ProductResponse()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency.Code,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CreatedOnUtc = product.CreatedOnUtc,
            UpdatedOnUtc = product.UpdatedOnUtc,
        }).ToList();

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
