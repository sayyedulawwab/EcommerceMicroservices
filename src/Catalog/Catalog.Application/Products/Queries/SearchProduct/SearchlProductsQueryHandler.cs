using Catalog.Application.Abstractions.Messaging;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using System.Linq.Expressions;

namespace Catalog.Application.Products.Queries.SearchProduct;
internal sealed class SearchlProductsQueryHandler : IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>>
{
    private readonly IProductRepository _productRepository;
    public SearchlProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    public async Task<Result<PagedList<ProductResponse>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {

        // Build filter expression dynamically
        Expression<Func<Product, bool>>? filter = product =>
            (!request.categoryId.HasValue || product.CategoryId == request.categoryId) &&
            (!request.minPrice.HasValue || product.Price.Amount >= request.minPrice) &&
            (!request.maxPrice.HasValue || product.Price.Amount <= request.maxPrice) &&
            (string.IsNullOrEmpty(request.keyword) || request.keyword == null);

        // sorting
        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = request.sortOrder?.ToLower() switch
        {
            "desc" when !string.IsNullOrEmpty(request.sortColumn) =>
                query => query.OrderByDescending(GetSortExpression(request.sortColumn)),

            _ when !string.IsNullOrEmpty(request.sortColumn) =>
                query => query.OrderBy(GetSortExpression(request.sortColumn)),

            _ => query => query.OrderBy(p => p.CreatedOn) // Default sorting by CreatedOn if sortColumn is null/empty
        };

        // Fetch products with applied filter, sorting, and pagination
        var (products, totalRecords) = await _productRepository.FindAsync(
            filter, orderBy, request.page, request.pageSize, cancellationToken);


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
            CreatedOn = product.CreatedOn,
            UpdatedOn = product.UpdatedOn,
        }).ToList();

        var pagedProducts = PagedList<ProductResponse>.Create(productResponse, request.page, request.pageSize, totalRecords);

        return pagedProducts;
    }


    private static Expression<Func<Product, object>> GetSortExpression(string sortColumn) =>
        sortColumn switch
        {
            "Name" => p => p.Name,
            "PriceAmount" => p => p.Price.Amount,
            _ => p => p.CreatedOn, // Default to CreatedOn if invalid
        };
}