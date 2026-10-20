using SharedKernel.Domain;

namespace Ordering.Domain.Orders;

public sealed record OrderLine(long ProductId, int Quantity);

public sealed record PricedOrderLine(long ProductId, string ProductName, Money Price);
