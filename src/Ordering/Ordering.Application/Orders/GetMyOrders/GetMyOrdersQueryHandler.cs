using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetMyOrders;

internal sealed class GetMyOrdersQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        List<Order> orders = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == request.UserId)
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderResponse.FromOrder).ToList();
    }
}
