using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories.AddCategory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Categories.AddCategory;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class AddCategoryController() : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, ICommandHandler<AddCategoryCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new AddCategoryCommand(request.Name, request.Description, request.ParentCategoryId);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }
}