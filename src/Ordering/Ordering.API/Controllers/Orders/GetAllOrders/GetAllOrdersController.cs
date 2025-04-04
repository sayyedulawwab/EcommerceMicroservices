using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.API.Extensions;
using Ordering.Application.Orders.GetAllOrders;
using SharedKernel.Domain;

namespace Ordering.API.Controllers.Orders.GetAllOrders;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/orders")]
[ApiController]
public class GetAllOrdersController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();

        Result<IReadOnlyList<OrderResponse>> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}