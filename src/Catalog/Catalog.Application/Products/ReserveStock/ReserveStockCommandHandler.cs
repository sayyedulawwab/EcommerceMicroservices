using Catalog.Application.Abstractions.Clock;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.ReserveStock;

internal sealed class ReserveStockCommandHandler(
    IProductRepository productRepository,
    IStockReservationRepository stockReservationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReserveStockCommand, ReserveStockResult>
{
    public async Task<Result<ReserveStockResult>> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        // The same product may appear more than once in an order; reserve the combined quantity.
        var requestedQuantities = request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        IReadOnlyList<Product> products = await productRepository.GetByIdsAsync(requestedQuantities.Keys, cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        IReadOnlyList<StockReservation> existingReservations = await stockReservationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);

        if (existingReservations.Count > 0)
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

        if (products.Select(product => product.Price.Currency).Distinct().Count() > 1)
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

            stockReservationRepository.Add(StockReservation.Create(request.OrderId, productId, quantity, dateTimeProvider.UtcNow));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

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
