using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Products.AddProduct;
using Catalog.Application.Products.DeleteProduct;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Products.DeleteProduct;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class DeleteProductController() : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(long id, ICommandHandler<DeleteProductCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}