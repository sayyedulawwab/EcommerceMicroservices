using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Products.ReleaseStock;
using Catalog.Domain.Products;
using Moq;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests.Products;

public sealed class ReleaseStockCommandHandlerTests
{
    private const long OrderId = 7;

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IStockReservationRepository> _reservationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly ReleaseStockCommandHandler _handler;

    public ReleaseStockCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(TestEntities.Now);

        _handler = new ReleaseStockCommandHandler(
            _productRepository.Object,
            _reservationRepository.Object,
            _unitOfWork.Object,
            _clock.Object);
    }

    private void SetupReservations(params StockReservation[] reservations) =>
        _reservationRepository
            .Setup(r => r.GetByOrderIdAsync(OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservations);

    [Fact]
    public async Task Handle_WithReservations_AddsQuantitiesBackMarksReservationsReleasedAndSaves()
    {
        Product first = TestEntities.NewProduct(1, quantity: 2);
        Product second = TestEntities.NewProduct(2, quantity: 0);
        StockReservation firstReservation = TestEntities.NewReservation(OrderId, 1, 3);
        StockReservation secondReservation = TestEntities.NewReservation(OrderId, 2, 4);
        SetupReservations(firstReservation, secondReservation);
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        Result result = await _handler.Handle(new ReleaseStockCommand(OrderId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, first.Quantity);
        Assert.Equal(4, second.Quantity);
        Assert.Equal(StockReservationStatus.Released, firstReservation.Status);
        Assert.Equal(StockReservationStatus.Released, secondReservation.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CalledTwice_ReleasesTheStockOnlyOnce()
    {
        Product product = TestEntities.NewProduct(1, quantity: 2);
        StockReservation reservation = TestEntities.NewReservation(OrderId, 1, 3);
        SetupReservations(reservation);
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        await _handler.Handle(new ReleaseStockCommand(OrderId), TestContext.Current.CancellationToken);
        Result second = await _handler.Handle(new ReleaseStockCommand(OrderId), TestContext.Current.CancellationToken);

        Assert.True(second.IsSuccess);
        Assert.Equal(5, product.Quantity);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CommittedReservation_IsNotReleased()
    {
        StockReservation reservation = TestEntities.NewReservation(OrderId, 1, 3);
        reservation.Commit(TestEntities.Now);
        SetupReservations(reservation);

        Result result = await _handler.Handle(new ReleaseStockCommand(OrderId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(StockReservationStatus.Committed, reservation.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNoLongerExists_StillReleasesReservation()
    {
        StockReservation reservation = TestEntities.NewReservation(OrderId, 1, 3);
        SetupReservations(reservation);
        _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Result result = await _handler.Handle(new ReleaseStockCommand(OrderId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(StockReservationStatus.Released, reservation.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoReservations_ReturnsSuccessWithoutSaving()
    {
        SetupReservations();

        Result result = await _handler.Handle(new ReleaseStockCommand(OrderId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _productRepository.Verify(
            r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
