using SharedKernel.Domain;

namespace Catalog.Domain.Products;

/// <summary>
/// Stock taken out of <see cref="Product.Quantity"/> for an order. Released (added back) when the order is cancelled,
/// committed when it is paid. Rows are kept after that so a redelivered order is recognised and never reserved twice.
/// </summary>
public sealed class StockReservation : Entity<long>
{
    private StockReservation(long orderId, long productId, int quantity, DateTime createdOnUtc)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        Status = StockReservationStatus.Reserved;
        CreatedOnUtc = createdOnUtc;
    }

    private StockReservation()
    {
    }

    public long OrderId { get; private set; }
    public long ProductId { get; private set; }
    public int Quantity { get; private set; }
    public StockReservationStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public bool IsActive => Status == StockReservationStatus.Reserved;

    public static StockReservation Create(long orderId, long productId, int quantity, DateTime createdOnUtc)
    {
        return new StockReservation(orderId, productId, quantity, createdOnUtc);
    }

    public void Release(DateTime utcNow) => Close(StockReservationStatus.Released, utcNow);

    public void Commit(DateTime utcNow) => Close(StockReservationStatus.Committed, utcNow);

    private void Close(StockReservationStatus status, DateTime utcNow)
    {
        if (!IsActive)
        {
            return;
        }

        Status = status;
        UpdatedOnUtc = utcNow;
    }
}

public enum StockReservationStatus
{
    Reserved = 1,
    Released = 2,
    Committed = 3,
}
