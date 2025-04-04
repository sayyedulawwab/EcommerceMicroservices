using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories;
using Catalog.Application.Categories.GetAllCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Categories.GetAllCategories;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class GetAllCategoriesController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllCategoriesQuery();

        Result<IReadOnlyList<CategoryResponse>> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}