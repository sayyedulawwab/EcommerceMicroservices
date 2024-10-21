﻿using Cart.Application.Abstractions.Caching;
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
        return await _cacheService.GetAsync<Domain.Carts.Cart>($"cart-{userId}");
    }

    public async Task RemoveAsync(Domain.Carts.Cart cart)
    {
        await _cacheService.RemoveAsync($"cart-{cart.UserId}");
    }
}
