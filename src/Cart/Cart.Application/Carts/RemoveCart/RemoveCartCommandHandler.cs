using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Carts;
using SharedKernel.Domain;

namespace Cart.Application.Carts.RemoveCart;
internal sealed class RemoveCartCommandHandler : ICommandHandler<RemoveCartCommand>
{
    private readonly ICartRepository _cartRepository;

    public RemoveCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result> Handle(RemoveCartCommand request, CancellationToken cancellationToken)
    {
        await _cartRepository.RemoveAsync(request.UserId);

        return Result.Success();
    }
}
