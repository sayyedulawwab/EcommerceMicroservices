using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Repositories;
internal sealed class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public override async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Order>().Include(order => order.OrderItems).ToListAsync(cancellationToken);
    }

    public override async Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Order>()
            .Where(entity => entity.Id == id)
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
