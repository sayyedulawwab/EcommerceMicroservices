using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories.DeleteCategory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Categories.DeleteCategory;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class DeleteCategoryController() : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(long id, ICommandHandler<DeleteCategoryCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}