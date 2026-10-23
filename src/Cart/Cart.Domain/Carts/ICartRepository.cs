namespace Cart.Domain.Carts;

public interface ICartRepository
{
    Task<Cart?> GetByUserId(Guid userId, CancellationToken cancellationToken = default);
    Task SaveAsync(Cart cart, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default);
}
