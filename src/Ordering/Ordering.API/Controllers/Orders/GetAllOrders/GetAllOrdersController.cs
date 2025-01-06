using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.API.Extensions;
using Ordering.Application.Orders.GetAllOrders;
using SharedKernel.Domain;

namespace Ordering.API.Controllers.Orders.GetAllOrders;
[Route("api/orders")]
[ApiController]
public class GetAllOrdersController : ControllerBase
{
    private readonly ISender _sender;

    public GetAllOrdersController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();

        Result<IReadOnlyList<OrderResponse>> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
