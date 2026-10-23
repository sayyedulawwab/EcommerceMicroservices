using Cart.Application.Abstractions.Caching;
using Cart.Domain.Carts;

namespace Cart.Infrastructure.Repositories;

internal sealed class CartRepository(ICacheService cacheService) : ICartRepository
{
    public async Task SaveAsync(Domain.Carts.Cart cart, CancellationToken cancellationToken = default)
    {
        await cacheService.SetAsync<Domain.Carts.Cart>(Key(cart.UserId), cart, cancellationToken);
    }

    public async Task<Domain.Carts.Cart?> GetByUserId(Guid userId, CancellationToken cancellationToken = default)
    {
        Domain.Carts.Cart? cart = await cacheService.GetAsync<Domain.Carts.Cart>(Key(userId), cancellationToken);

        return cart;
    }

    public async Task RemoveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await cacheService.RemoveAsync(Key(userId), cancellationToken);
    }

    private static string Key(Guid userId) => $"cart-{userId}";
}
