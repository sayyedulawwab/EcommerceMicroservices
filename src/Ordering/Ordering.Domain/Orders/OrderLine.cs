using SharedKernel.Domain;

namespace Ordering.Domain.Orders;

public sealed record OrderLine(Guid ProductId, int Quantity);

public sealed record PricedOrderLine(Guid ProductId, string ProductName, Money Price);
