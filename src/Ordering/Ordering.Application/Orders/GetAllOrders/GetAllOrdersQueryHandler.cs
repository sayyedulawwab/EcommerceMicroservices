using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetAllOrders;

internal sealed class GetAllOrdersQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        List<Order> orders = await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderResponse.FromOrder).ToList();
    }
}
