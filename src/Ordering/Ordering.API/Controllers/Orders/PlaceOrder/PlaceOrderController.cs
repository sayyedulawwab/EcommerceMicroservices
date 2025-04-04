using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.API.Extensions;
using Ordering.Application.Orders.PlaceOrder;
using SharedKernel.Domain;
using System.Globalization;
using System.Security.Claims;

namespace Ordering.API.Controllers.Orders.PlaceOrder;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/orders")]
[ApiController]
public class PlaceOrderController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        Claim? userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            return Unauthorized("User ID claim is missing.");
        }

        long userId = long.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);

        var orderItems = request.OrderItems.Select(item =>
            new OrderItemCommand(item.ProductId, item.ProductName, item.PriceAmount, item.PriceCurrency, item.Quantity))
            .ToList();

        var command = new PlaceOrderCommand(userId, orderItems);

        Result<long> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }
        return Created(string.Empty, result.Value);
    }
}