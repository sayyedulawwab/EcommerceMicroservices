using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using System.Globalization;
using System.Linq.Expressions;

namespace Catalog.Application.Products.SearchProduct;
internal sealed class SearchlProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>>
{
    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {


        Expression<Func<Product, bool>>? filter = product =>
            (!request.CategoryId.HasValue || product.CategoryId == request.CategoryId) &&
            (!request.MinPrice.HasValue || product.Price.Amount >= request.MinPrice) &&
            (!request.MaxPrice.HasValue || product.Price.Amount <= request.MaxPrice) &&
            (string.IsNullOrEmpty(request.Keyword) || request.Keyword == null);


        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = request.SortOrder?.ToUpperInvariant() switch
        {
            "DESC" when !string.IsNullOrEmpty(request.SortColumn) =>
                query => query.OrderByDescending(GetSortExpression(request.SortColumn)),

            _ when !string.IsNullOrEmpty(request.SortColumn) =>
                query => query.OrderBy(GetSortExpression(request.SortColumn)),

            _ => query => query.OrderBy(p => p.CreatedOnUtc)
        };


        (IReadOnlyList<Product> products, int totalRecords) = await productRepository.FindAsync(
            filter, orderBy, request.Page, request.PageSize, cancellationToken);


        if (products is null)
        {
            return Result.Failure<PagedList<ProductResponse>>(ProductErrors.NotFound());
        }

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

        var pagedProducts = PagedList<ProductResponse>.Create(productResponse, request.Page, request.PageSize, totalRecords);

        return pagedProducts;
    }


    private static Expression<Func<Product, object>> GetSortExpression(string sortColumn) =>
    sortColumn switch
    {
        "Name" => p => p.Name,
        "PriceAmount" => p => p.Price.Amount,
        _ => p => p.CreatedOnUtc, // Default to CreatedOnUtc if invalid
    };
}