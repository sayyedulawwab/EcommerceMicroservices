using Catalog.API.Extensions;
using Catalog.Application.Categories.EditCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Categories.EditCategory;
[Route("api/categories")]
[ApiController]
public class EditCategoryController : ControllerBase
{
    private readonly ISender _sender;
    public EditCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(long id, EditCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.name, request.description, request.parentCategoryId);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
