using Cart.Application.Abstractions.Caching;
using Cart.Domain.Carts;

namespace Cart.Infrastructure.Repositories;
internal sealed class CartRepository(ICacheService cacheService) : ICartRepository
{
    public async Task AddAsync(Domain.Carts.Cart cart)
    {
        await cacheService.SetAsync<Domain.Carts.Cart>($"cart-{cart.UserId}", cart);
    }

    public async Task<Domain.Carts.Cart?> GetByUserId(long userId, CancellationToken cancellationToken = default)
    {
        Domain.Carts.Cart? cart = await cacheService.GetAsync<Domain.Carts.Cart>($"cart-{userId}", cancellationToken);

        return cart;
    }

    public async Task RemoveAsync(long userId)
    {
        await cacheService.RemoveAsync($"cart-{userId}");
    }
}