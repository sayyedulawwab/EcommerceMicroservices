using Cart.Domain.Carts;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Cart.Application.Carts.RemoveCart;
internal sealed class RemoveCartCommandHandler(ICartRepository cartRepository) : ICommandHandler<RemoveCartCommand>
{
    public async Task<Result> Handle(RemoveCartCommand request, CancellationToken cancellationToken)
    {
        await cartRepository.RemoveAsync(request.UserId);

        return Result.Success();
    }
}
