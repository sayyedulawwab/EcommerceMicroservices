namespace Cart.Domain.Carts;

public interface ICartRepository
{
    Task<Cart?> GetByUserId(long userId, CancellationToken cancellationToken = default);
    Task SaveAsync(Cart cart, CancellationToken cancellationToken = default);
    Task RemoveAsync(long userId, CancellationToken cancellationToken = default);
}
