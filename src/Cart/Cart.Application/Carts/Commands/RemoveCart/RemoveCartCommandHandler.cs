using Cart.Application.Abstractions.Clock;
using Cart.Application.Abstractions.Messaging;
using Cart.Domain.Carts;
using SharedKernel.Domain;

namespace Cart.Application.Carts.Commands.RemoveCart;
internal sealed class RemoveCartCommandHandler : ICommandHandler<RemoveCartCommand>
{
    private readonly ICartRepository _cartRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveCartCommandHandler(ICartRepository cartRepository, IDateTimeProvider dateTimeProvider)
    {
        _cartRepository = cartRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveCartCommand request, CancellationToken cancellationToken)
    {
        await _cartRepository.RemoveAsync(request.userId);

        return Result.Success();
    }
}
