namespace Ordering.Domain.Orders;
public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    void Add(Order order);
    void Update(Order order);
    void Remove(Order order);
}