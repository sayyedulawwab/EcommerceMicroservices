using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Products.ReserveStock;
using Catalog.Domain.Products;
using Moq;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Catalog.Application.UnitTests.Products;

public sealed class ReserveStockCommandHandlerTests
{
    private const long OrderId = 42;

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStockReservationRepository> _reservationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly List<StockReservation> _addedReservations = [];
    private readonly ReserveStockCommandHandler _handler;

    public ReserveStockCommandHandlerTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(TestEntities.Now);
        _reservationRepository
            .Setup(r => r.GetByOrderIdAsync(OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _reservationRepository
            .Setup(r => r.Add(It.IsAny<StockReservation>()))
            .Callback<StockReservation>(_addedReservations.Add);

        _handler = new ReserveStockCommandHandler(
            _productRepository.Object,
            _reservationRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object);
    }

    private void SetupProducts(params Product[] products) =>
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

    private Task<Result<ReserveStockResult>> HandleAsync(params OrderStockItem[] items) =>
        _handler.Handle(new ReserveStockCommand(OrderId, [.. items]), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_AllItemsInStock_ReservesEachProductAndSaves()
    {
        Product first = TestEntities.NewProduct(1, quantity: 10);
        Product second = TestEntities.NewProduct(2, quantity: 5);
        SetupProducts(first, second);

        Result<ReserveStockResult> result = await HandleAsync(new OrderStockItem(1, 3), new OrderStockItem(2, 5));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReserved);
        Assert.Equal(7, first.Quantity);
        Assert.Equal(0, second.Quantity);

        Assert.Equal(2, _addedReservations.Count);
        Assert.Contains(_addedReservations, r => r.OrderId == OrderId && r.ProductId == 1 && r.Quantity == 3);
        Assert.Contains(_addedReservations, r => r.OrderId == OrderId && r.ProductId == 2 && r.Quantity == 5);
        Assert.All(_addedReservations, r => Assert.Equal(TestEntities.Now, r.CreatedOnUtc));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateProductLines_SumsQuantityIntoOneReservation()
    {
        Product product = TestEntities.NewProduct(1, quantity: 10);
        SetupProducts(product);

        Result<ReserveStockResult> result = await HandleAsync(new OrderStockItem(1, 3), new OrderStockItem(1, 4));

        Assert.True(result.Value.IsReserved);
        Assert.Equal(3, product.Quantity);

        StockReservation reservation = Assert.Single(_addedReservations);
        Assert.Equal(1, reservation.ProductId);
        Assert.Equal(7, reservation.Quantity);

        PricedOrderItem item = Assert.Single(result.Value.Items);
        Assert.Equal(7, item.Quantity);
    }

    [Fact]
    public async Task Handle_DuplicateLinesExceedingStockTogether_RejectsProduct()
    {
        Product product = TestEntities.NewProduct(1, quantity: 5);
        SetupProducts(product);

        Result<ReserveStockResult> result = await HandleAsync(new OrderStockItem(1, 3), new OrderStockItem(1, 3));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReserved);
        Assert.Equal([1L], result.Value.RejectedProductIds);
        Assert.Equal(5, product.Quantity);
        AssertNothingReservedOrSaved();
    }

    [Fact]
    public async Task Handle_UnknownProduct_RejectsWithProductIdAndSavesNothing()
    {
        Product known = TestEntities.NewProduct(1, quantity: 10);
        SetupProducts(known);

        Result<ReserveStockResult> result = await HandleAsync(new OrderStockItem(1, 2), new OrderStockItem(99, 1));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReserved);
        Assert.Equal([99L], result.Value.RejectedProductIds);
        Assert.Empty(result.Value.Items);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RejectionReason));
        Assert.Equal(10, known.Quantity);
        AssertNothingReservedOrSaved();
    }

    [Fact]
    public async Task Handle_InsufficientStock_RejectsAllShortProductsAndKeepsStock()
    {
        Product inStock = TestEntities.NewProduct(1, quantity: 10);
        Product shortOne = TestEntities.NewProduct(2, quantity: 1);
        Product shortTwo = TestEntities.NewProduct(3, quantity: 0);
        SetupProducts(inStock, shortOne, shortTwo);

        Result<ReserveStockResult> result = await HandleAsync(
            new OrderStockItem(1, 2),
            new OrderStockItem(2, 2),
            new OrderStockItem(3, 1));

        Assert.False(result.Value.IsReserved);
        Assert.Equal([2L, 3L], result.Value.RejectedProductIds.Order());
        Assert.Equal(10, inStock.Quantity);
        Assert.Equal(1, shortOne.Quantity);
        Assert.Equal(0, shortTwo.Quantity);
        AssertNothingReservedOrSaved();
    }

    [Fact]
    public async Task Handle_ProductsInDifferentCurrencies_RejectsAndSavesNothing()
    {
        Product usd = TestEntities.NewProduct(1, quantity: 10, currency: Currency.Usd);
        Product bdt = TestEntities.NewProduct(2, quantity: 10, currency: Currency.Bdt);
        SetupProducts(usd, bdt);

        Result<ReserveStockResult> result = await HandleAsync(new OrderStockItem(1, 1), new OrderStockItem(2, 1));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReserved);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RejectionReason));
        Assert.Equal(10, usd.Quantity);
        Assert.Equal(10, bdt.Quantity);
        AssertNothingReservedOrSaved();
    }

    [Fact]
    public async Task Handle_ExistingReservationsForOrder_ReturnsReservedWithoutReservingAgain()
    {
        Product product = TestEntities.NewProduct(1, quantity: 7, price: 25m, name: "Mouse");
        SetupProducts(product);
        _reservationRepository
            .Setup(r => r.GetByOrderIdAsync(OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestEntities.NewReservation(OrderId, 1, 3)]);

        Result<ReserveStockResult> result = await HandleAsync(new OrderStockItem(1, 3));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReserved);
        Assert.Equal(7, product.Quantity);

        PricedOrderItem item = Assert.Single(result.Value.Items);
        Assert.Equal(1, item.ProductId);
        Assert.Equal("Mouse", item.ProductName);
        Assert.Equal(25m, item.PriceAmount);
        Assert.Equal(3, item.Quantity);

        AssertNothingReservedOrSaved();
    }

    [Fact]
    public async Task Handle_Reserved_PricedItemsCarryCatalogNameAndPrice()
    {
        Product keyboard = TestEntities.NewProduct(1, quantity: 10, price: 49.99m, currency: Currency.Bdt, name: "Keyboard");
        Product monitor = TestEntities.NewProduct(2, quantity: 10, price: 199m, currency: Currency.Bdt, name: "Monitor");
        SetupProducts(keyboard, monitor);

        Result<ReserveStockResult> result = await HandleAsync(new OrderStockItem(1, 2), new OrderStockItem(2, 1));

        Assert.True(result.Value.IsReserved);
        Assert.Equal(2, result.Value.Items.Count);

        PricedOrderItem keyboardItem = Assert.Single(result.Value.Items, i => i.ProductId == 1);
        Assert.Equal("Keyboard", keyboardItem.ProductName);
        Assert.Equal(49.99m, keyboardItem.PriceAmount);
        Assert.Equal("BDT", keyboardItem.PriceCurrency);
        Assert.Equal(2, keyboardItem.Quantity);

        PricedOrderItem monitorItem = Assert.Single(result.Value.Items, i => i.ProductId == 2);
        Assert.Equal("Monitor", monitorItem.ProductName);
        Assert.Equal(199m, monitorItem.PriceAmount);
        Assert.Equal("BDT", monitorItem.PriceCurrency);
        Assert.Equal(1, monitorItem.Quantity);
    }

    [Fact]
    public async Task Handle_RequestsDistinctProductIds()
    {
        IReadOnlyCollection<long>? requestedIds = null;
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<long>, CancellationToken>((ids, _) => requestedIds = [.. ids])
            .ReturnsAsync([TestEntities.NewProduct(1, quantity: 10), TestEntities.NewProduct(2, quantity: 10)]);

        await HandleAsync(new OrderStockItem(1, 1), new OrderStockItem(2, 1), new OrderStockItem(1, 1));

        Assert.NotNull(requestedIds);
        Assert.Equal([1L, 2L], requestedIds.Order());
    }

    private void AssertNothingReservedOrSaved()
    {
        Assert.Empty(_addedReservations);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
