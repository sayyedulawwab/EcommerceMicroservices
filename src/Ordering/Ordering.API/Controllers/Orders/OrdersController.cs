using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.Orders.Commands.PlaceOrder;
using Ordering.Application.Orders.Queries.GetAllOrders;
using System.Security.Claims;

namespace Ordering.API.Controllers.Orders;
[Route("api/orders")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
        var userId = Int64.Parse(userIdClaim.Value);
      

        var orderItems = request.orderItems.Select(item =>
            new OrderItemCommand(item.productId, item.productName, item.priceAmount, item.priceCurrency, item.quantity))
            .ToList();

        var command = new PlaceOrderCommand(userId, orderItems);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Created(string.Empty, result.Value);
    }

}