using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Application.Orders.Commands.UpdateOrderStatus;
internal sealed class UpdateOrderStatusToCanceledCommandHandler : ICommandHandler<UpdateOrderStatusToCanceledCommand, long>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageSession _messageSession;

    public UpdateOrderStatusToCanceledCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider, IMessageSession messageSession)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _messageSession = messageSession;
    }

    public async Task<Result<long>> Handle(UpdateOrderStatusToCanceledCommand request, CancellationToken cancellationToken)
    {

        var order = await _orderRepository.GetByIdAsync(request.orderId);

        if (order is null)
        {
            return Result.Failure<long>(OrderErrors.NotFound());
        }

        var updatedOrder = Order.Update(order, order.UserId, order.TotalPrice, OrderStatus.Cancelled, _dateTimeProvider.UtcNow);

        _orderRepository.Update(updatedOrder);

        await _unitOfWork.SaveChangesAsync();

        return order.Id;
    }
}
