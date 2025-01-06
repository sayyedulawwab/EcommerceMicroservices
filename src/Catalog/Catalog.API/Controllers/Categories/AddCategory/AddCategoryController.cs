using Catalog.API.Extensions;
using Catalog.Application.Categories.AddCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Categories.AddCategory;
[Route("api/categories")]
[ApiController]
public class AddCategoryController : ControllerBase
{
    private readonly ISender _sender;
    public AddCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCategoryCommand(request.Name, request.Description, request.ParentCategoryId);

        Result<long> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }
}
