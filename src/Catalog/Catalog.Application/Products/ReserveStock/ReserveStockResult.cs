using SharedKernel.Events;

namespace Catalog.Application.Products.ReserveStock;

public sealed record ReserveStockResult(
    bool IsReserved,
    IReadOnlyList<PricedOrderItem> Items,
    string? RejectionReason,
    IReadOnlyList<long> RejectedProductIds)
{
    public static ReserveStockResult Reserved(IReadOnlyList<PricedOrderItem> items) =>
        new(true, items, null, []);

    public static ReserveStockResult Rejected(string reason, IReadOnlyList<long> rejectedProductIds) =>
        new(false, [], reason, rejectedProductIds);
}
