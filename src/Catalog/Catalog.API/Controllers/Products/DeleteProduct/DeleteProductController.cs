using Catalog.API.Extensions;
using Catalog.Application.Products.DeleteProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Products.DeleteProduct;
[Route("api/products")]
[ApiController]
public class DeleteProductController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteProductController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        Result<long> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
