using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories.EditCategory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Categories.EditCategory;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class EditCategoryController() : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(long id, EditCategoryRequest request, ICommandHandler<EditCategoryCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.Name, request.Description, request.ParentCategoryId);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}