using Cart.Application.Abstractions.Caching;
using Cart.Domain.Carts;

namespace Cart.Infrastructure.Repositories;
internal sealed class CartRepository : ICartRepository
{
    private readonly ICacheService _cacheService;
    public CartRepository(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task AddAsync(Domain.Carts.Cart cart)
    {
        await _cacheService.SetAsync<Domain.Carts.Cart>($"cart-{cart.UserId}", cart);
    }

    public async Task<Domain.Carts.Cart?> GetByUserId(long userId, CancellationToken cancellationToken = default)
    {
        var cart = await _cacheService.GetAsync<Domain.Carts.Cart>($"cart-{userId}");

        return cart;
    }

    public async Task RemoveAsync(long userId)
    {
        await _cacheService.RemoveAsync($"cart-{userId}");
    }
}
