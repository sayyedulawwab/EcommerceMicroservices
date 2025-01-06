using Catalog.API.Extensions;
using Catalog.Application.Categories.DeleteCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Categories.DeleteCategory;
[Route("api/categories")]
[ApiController]
public class DeleteCategoryController : ControllerBase
{
    private readonly ISender _sender;
    public DeleteCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<long> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
