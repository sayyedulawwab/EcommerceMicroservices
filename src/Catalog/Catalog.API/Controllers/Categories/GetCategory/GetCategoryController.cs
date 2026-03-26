using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories;
using Catalog.Application.Categories.GetAllCategories;
using Catalog.Application.Categories.GetCategoryById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Categories.GetCategory;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
public class GetCategoryController() : ControllerBase
{
    [MapToApiVersion(1)]
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(long id, IQueryHandler<GetCategoryByIdQuery, CategoryResponse> handler, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        Result<CategoryResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}