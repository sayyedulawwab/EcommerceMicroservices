using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories.DeleteCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Categories.DeleteCategory;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class DeleteCategoryController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<long> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}