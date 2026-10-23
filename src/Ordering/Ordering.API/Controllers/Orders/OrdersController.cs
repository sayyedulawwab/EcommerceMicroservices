using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.API.Extensions;
using Ordering.Application.Orders;
using Ordering.Application.Orders.CancelOrder;
using Ordering.Application.Orders.DeliverOrder;
using Ordering.Application.Orders.GetAllOrders;
using Ordering.Application.Orders.GetMyOrders;
using Ordering.Application.Orders.GetOrderById;
using Ordering.Application.Orders.PlaceOrder;
using Ordering.Application.Orders.ShipOrder;
using SharedKernel.Auth;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.API.Controllers.Orders;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/orders")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private const string CancelledByCustomerReason = "Cancelled by customer";

    /// <summary>
    /// Places an order. Product names and prices come from Catalog, so only product ids and quantities are sent.
    /// The order is confirmed (or cancelled) asynchronously; poll GET /orders/{id} for its status.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, ICommandHandler<PlaceOrderCommand, Guid> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not Guid userId)
        {
            return Unauthorized();
        }

        var orderItems = (request.OrderItems ?? [])
            .Select(item => new OrderItemCommand(item.ProductId, item.Quantity))
            .ToList();

        var command = new PlaceOrderCommand(userId, orderItems);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Accepted(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderResponse>> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not Guid userId)
        {
            return Unauthorized();
        }

        Result<IReadOnlyList<OrderResponse>> result = await handler.Handle(new GetMyOrdersQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllOrders(IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();

        Result<IReadOnlyList<OrderResponse>> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, IQueryHandler<GetOrderByIdQuery, OrderResponse> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not Guid userId)
        {
            return Unauthorized();
        }

        var query = new GetOrderByIdQuery(id, userId, User.IsInRole(Roles.Admin));

        Result<OrderResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>Cancels one of the caller's orders. Only possible before it is paid.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, ICommandHandler<CancelOrderCommand> handler, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not Guid userId)
        {
            return Unauthorized();
        }

        Result result = await handler.Handle(new CancelOrderCommand(id, CancelledByCustomerReason, userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:guid}/ship")]
    public async Task<IActionResult> ShipOrder(Guid id, ICommandHandler<ShipOrderCommand> handler, CancellationToken cancellationToken)
    {
        Result result = await handler.Handle(new ShipOrderCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:guid}/deliver")]
    public async Task<IActionResult> DeliverOrder(Guid id, ICommandHandler<DeliverOrderCommand> handler, CancellationToken cancellationToken)
    {
        Result result = await handler.Handle(new DeliverOrderCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return NoContent();
    }
}
