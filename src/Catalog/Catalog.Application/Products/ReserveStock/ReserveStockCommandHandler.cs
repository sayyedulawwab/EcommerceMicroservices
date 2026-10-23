using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.ReserveStock;

internal sealed class ReserveStockCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReserveStockCommand, ReserveStockResult>
{
    public async Task<Result<ReserveStockResult>> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        // The same product may appear more than once in an order; reserve the combined quantity.
        var requestedQuantities = request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var requestedProductIds = requestedQuantities.Keys.ToList();

        Dictionary<long, Product> productsById = await dbContext.Products
            .Where(product => requestedProductIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        bool alreadyReserved = await dbContext.StockReservations
            .AnyAsync(reservation => reservation.OrderId == request.OrderId, cancellationToken);

        if (alreadyReserved)
        {
            // The order was already handled by an earlier delivery (and may since be released or committed): never reserve twice.
            return ReserveStockResult.Reserved(ToPricedItems(requestedQuantities, productsById));
        }

        var rejectedProductIds = requestedQuantities
            .Where(requested => !productsById.TryGetValue(requested.Key, out Product? product) || !product.HasStock(requested.Value))
            .Select(requested => requested.Key)
            .ToList();

        if (rejectedProductIds.Count > 0)
        {
            return ReserveStockResult.Rejected("One or more products are unknown or out of stock", rejectedProductIds);
        }

        if (productsById.Values.Select(product => product.Price.Currency).Distinct().Count() > 1)
        {
            return ReserveStockResult.Rejected("The products in the order are priced in different currencies", []);
        }

        foreach ((long productId, int quantity) in requestedQuantities)
        {
            Result reserveResult = productsById[productId].ReserveStock(quantity);

            if (reserveResult.IsFailure)
            {
                return Result.Failure<ReserveStockResult>(reserveResult.Error);
            }

            dbContext.StockReservations.Add(StockReservation.Create(request.OrderId, productId, quantity, dateTimeProvider.UtcNow));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ReserveStockResult.Reserved(ToPricedItems(requestedQuantities, productsById));
    }

    private static List<PricedOrderItem> ToPricedItems(
        Dictionary<long, int> requestedQuantities,
        Dictionary<long, Product> productsById)
    {
        return requestedQuantities
            .Where(requested => productsById.ContainsKey(requested.Key))
            .Select(requested =>
            {
                Product product = productsById[requested.Key];

                return new PricedOrderItem(
                    product.Id,
                    product.Name,
                    product.Price.Amount,
                    product.Price.Currency.Code,
                    requested.Value);
            })
            .ToList();
    }
}
