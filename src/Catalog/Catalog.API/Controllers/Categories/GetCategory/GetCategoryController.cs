using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories;
using Catalog.Application.Categories.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Categories.GetCategory;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class GetCategoryController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(long id, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        Result<CategoryResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}