using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.API.Extensions;
using Ordering.Application.Orders.PlaceOrder;
using System.Security.Claims;

namespace Ordering.API.Controllers.Orders.PlaceOrder;
[Route("api/orders")]
[ApiController]
public class PlaceOrderController : ControllerBase
{
    private readonly ISender _sender;

    public PlaceOrderController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
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
            return result.Error.ToActionResult();
        }
        return Created(string.Empty, result.Value);
    }
}
