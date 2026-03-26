using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories;
using Catalog.Application.Categories.GetAllCategories;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Categories.GetAllCategories;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class GetAllCategoriesController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> GetCategories(IQueryHandler<GetAllCategoriesQuery, IReadOnlyList<CategoryResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new GetAllCategoriesQuery();

        Result<IReadOnlyList<CategoryResponse>> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}