using SharedKernel.Domain;

namespace Catalog.Domain.Products;

// Reserving and releasing stock changes two aggregates: the product's quantity and the reservation record.
// Neither should reach into the other, so the rule lives here. The handler loads, saves and adds the new rows.
public static class StockReservationService
{
    public static Result<List<StockReservation>> Reserve(
        Guid orderId,
        IReadOnlyDictionary<ProductId, int> requestedQuantities,
        IReadOnlyDictionary<ProductId, Product> productsById,
        DateTime utcNow)
    {
        // Check every line before changing any product. The consumer's outbox saves the DbContext even when this fails,
        // so a product changed before a later line failed would lose stock.
        foreach ((ProductId productId, int quantity) in requestedQuantities)
        {
            if (!productsById.TryGetValue(productId, out Product? product))
            {
                return Result.Failure<List<StockReservation>>(ProductErrors.NotFound());
            }

            if (quantity <= 0)
            {
                return Result.Failure<List<StockReservation>>(ProductErrors.InvalidQuantity(productId.Value));
            }

            if (!product.HasStock(quantity))
            {
                return Result.Failure<List<StockReservation>>(ProductErrors.InsufficientStock(productId.Value));
            }
        }

        var reservations = new List<StockReservation>();

        foreach ((ProductId productId, int quantity) in requestedQuantities)
        {
            Product product = productsById[productId];

            Result reserveResult = product.ReserveStock(quantity);

            if (reserveResult.IsFailure)
            {
                return Result.Failure<List<StockReservation>>(reserveResult.Error);
            }

            reservations.Add(StockReservation.Create(orderId, productId, quantity, utcNow));
        }

        return reservations;
    }

    /// <summary>Gives the reserved stock back and closes the reservations. A product that no longer exists is skipped.</summary>
    public static void Release(
        IEnumerable<StockReservation> reservations,
        IReadOnlyDictionary<ProductId, Product> productsById,
        DateTime utcNow)
    {
        foreach (StockReservation reservation in reservations)
        {
            if (productsById.TryGetValue(reservation.ProductId, out Product? product))
            {
                product.ReleaseStock(reservation.Quantity);
            }

            reservation.Release(utcNow);
        }
    }
}
