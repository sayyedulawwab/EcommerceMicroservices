using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Carts;
using SharedKernel.Domain;

namespace Cart.Application.Carts.RemoveCart;
internal sealed class RemoveCartCommandHandler(ICartRepository cartRepository) : ICommandHandler<RemoveCartCommand>
{
    public async Task<Result> Handle(RemoveCartCommand request, CancellationToken cancellationToken)
    {
        await cartRepository.RemoveAsync(request.UserId);

        return Result.Success();
    }
}
