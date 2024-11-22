namespace Cart.Domain.Carts;
public interface ICartRepository
{
    Task<Cart?> GetByUserId(long userId, CancellationToken cancellationToken = default);
    Task AddAsync(Cart cart);
    Task RemoveAsync(long userId);
}