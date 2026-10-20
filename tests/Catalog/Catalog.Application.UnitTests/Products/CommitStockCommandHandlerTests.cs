using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Products.CommitStock;
using Catalog.Domain.Products;
using Moq;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests.Products;

public sealed class CommitStockCommandHandlerTests
{
    private const long OrderId = 11;

    private readonly Mock<IStockReservationRepository> _reservationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly CommitStockCommandHandler _handler;

    public CommitStockCommandHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(TestEntities.Now);

        _handler = new CommitStockCommandHandler(_reservationRepository.Object, _unitOfWork.Object, _clock.Object);
    }

    [Fact]
    public async Task Handle_WithReservations_CommitsAllAndSaves()
    {
        StockReservation first = TestEntities.NewReservation(OrderId, 1, 2);
        StockReservation second = TestEntities.NewReservation(OrderId, 2, 5);
        _reservationRepository
            .Setup(r => r.GetByOrderIdAsync(OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        Result result = await _handler.Handle(new CommitStockCommand(OrderId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(StockReservationStatus.Committed, first.Status);
        Assert.Equal(StockReservationStatus.Committed, second.Status);
        Assert.Equal(TestEntities.Now, first.UpdatedOnUtc);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoActiveReservations_ReturnsSuccessWithoutSaving()
    {
        StockReservation released = TestEntities.NewReservation(OrderId, 1, 2);
        released.Release(TestEntities.Now);
        _reservationRepository
            .Setup(r => r.GetByOrderIdAsync(OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([released]);

        Result result = await _handler.Handle(new CommitStockCommand(OrderId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(StockReservationStatus.Released, released.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
