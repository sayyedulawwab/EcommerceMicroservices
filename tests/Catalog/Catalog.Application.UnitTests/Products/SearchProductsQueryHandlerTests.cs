using System.Linq.Expressions;
using Catalog.Application.Products;
using Catalog.Application.Products.SearchProduct;
using Catalog.Domain.Products;
using Moq;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests.Products;

public sealed class SearchProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly SearchlProductsQueryHandler _handler;

    private readonly List<Product> _catalog =
    [
        TestEntities.NewProduct(1, quantity: 5, price: 20m, name: "Gaming Laptop", description: "Fast machine", categoryId: 1),
        TestEntities.NewProduct(2, quantity: 5, price: 15m, name: "Office Chair", description: "Ergonomic, fits a laptop desk", categoryId: 2),
        TestEntities.NewProduct(3, quantity: 5, price: 50m, name: "Desk Lamp", description: "LED light", categoryId: 2),
    ];

    private Expression<Func<Product, bool>>? _capturedPredicate;
    private Func<IQueryable<Product>, IOrderedQueryable<Product>>? _capturedOrderBy;
    private int _capturedPage;
    private int _capturedPageSize;

    public SearchProductsQueryHandlerTests()
    {
        _productRepository
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Product, bool>>?>(),
                It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<Product, bool>>?, Func<IQueryable<Product>, IOrderedQueryable<Product>>?, int, int, CancellationToken>(
                (predicate, orderBy, page, pageSize, _) =>
                {
                    _capturedPredicate = predicate;
                    _capturedOrderBy = orderBy;
                    _capturedPage = page;
                    _capturedPageSize = pageSize;
                })
            .ReturnsAsync(([], 0));

        _handler = new SearchlProductsQueryHandler(_productRepository.Object);
    }

    private static SearchProductsQuery Query(
        string? keyword = null,
        int page = 1,
        int pageSize = 10,
        long? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortColumn = null,
        string? sortOrder = null) =>
        new(categoryId, minPrice, maxPrice, keyword, page, pageSize, sortColumn, sortOrder);

    private async Task<List<long>> MatchingIdsAsync(SearchProductsQuery query)
    {
        await _handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.NotNull(_capturedPredicate);
        Func<Product, bool> predicate = _capturedPredicate.Compile();

        return [.. _catalog.Where(predicate).Select(p => p.Id).Order()];
    }

    [Fact]
    public async Task Handle_KeywordInName_MatchesProduct()
    {
        List<long> ids = await MatchingIdsAsync(Query(keyword: "Lamp"));

        Assert.Equal([3L], ids);
    }

    [Fact]
    public async Task Handle_KeywordInDescription_MatchesProduct()
    {
        List<long> ids = await MatchingIdsAsync(Query(keyword: "Ergonomic"));

        Assert.Equal([2L], ids);
    }

    [Fact]
    public async Task Handle_KeywordInNameOfOneAndDescriptionOfAnother_MatchesBoth()
    {
        List<long> ids = await MatchingIdsAsync(Query(keyword: "aptop"));

        Assert.Equal([1L, 2L], ids);
    }

    [Fact]
    public async Task Handle_KeywordWithSurroundingWhitespace_IsTrimmed()
    {
        List<long> ids = await MatchingIdsAsync(Query(keyword: "  Lamp  "));

        Assert.Equal([3L], ids);
    }

    [Fact]
    public async Task Handle_KeywordMatchingNothing_ReturnsNoProducts()
    {
        List<long> ids = await MatchingIdsAsync(Query(keyword: "Television"));

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_NoKeyword_MatchesEverything(string? keyword)
    {
        List<long> ids = await MatchingIdsAsync(Query(keyword: keyword));

        Assert.Equal([1L, 2L, 3L], ids);
    }

    [Fact]
    public async Task Handle_CategoryAndPriceRange_FiltersProducts()
    {
        List<long> ids = await MatchingIdsAsync(Query(categoryId: 2, minPrice: 10m, maxPrice: 20m));

        Assert.Equal([2L], ids);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    public async Task Handle_Page_IsAtLeastOne(int requestedPage, int expectedPage)
    {
        Result<PagedList<ProductResponse>> result =
            await _handler.Handle(Query(page: requestedPage), TestContext.Current.CancellationToken);

        Assert.Equal(expectedPage, _capturedPage);
        Assert.Equal(expectedPage, result.Value.Page);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 1)]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(10_000, 100)]
    public async Task Handle_PageSize_IsClampedBetweenOneAndHundred(int requestedPageSize, int expectedPageSize)
    {
        Result<PagedList<ProductResponse>> result =
            await _handler.Handle(Query(pageSize: requestedPageSize), TestContext.Current.CancellationToken);

        Assert.Equal(expectedPageSize, _capturedPageSize);
        Assert.Equal(expectedPageSize, result.Value.PageSize);
    }

    [Fact]
    public async Task Handle_SortByNameDescending_OrdersByNameDescending()
    {
        await _handler.Handle(Query(sortColumn: "name", sortOrder: "desc"), TestContext.Current.CancellationToken);

        Assert.NotNull(_capturedOrderBy);
        List<long> ids = [.. _capturedOrderBy(_catalog.AsQueryable()).Select(p => p.Id)];

        Assert.Equal([2L, 1L, 3L], ids);
    }

    [Fact]
    public async Task Handle_RepositoryResults_AreMappedToResponses()
    {
        Product product = _catalog[0];
        _productRepository
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<Product, bool>>?>(),
                It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([product], 21));

        Result<PagedList<ProductResponse>> result =
            await _handler.Handle(Query(page: 2, pageSize: 10), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(21, result.Value.TotalCount);
        Assert.True(result.Value.HasNextPage);
        Assert.True(result.Value.HasPreviousPage);

        ProductResponse response = Assert.Single(result.Value.Items);
        Assert.Equal(product.Id, response.Id);
        Assert.Equal(product.Name, response.Name);
        Assert.Equal(product.Price.Amount, response.PriceAmount);
        Assert.Equal("USD", response.PriceCurrency);
        Assert.Equal(product.CategoryId, response.CategoryId);
    }
}
