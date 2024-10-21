using Ordering.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Ordering.Infrastructure.Repositories;
internal sealed class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public override async Task<IReadOnlyList<Order?>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Order>().Include(order => order.OrderItems).ToListAsync(cancellationToken);
    }
}
