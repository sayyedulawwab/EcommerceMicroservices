using Catalog.API.Extensions;
using Catalog.Application.Categories.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Categories.GetCategory;
[Route("api/[controller]")]
[ApiController]
public class GetCategoryController : ControllerBase
{
    private readonly ISender _sender;

    public GetCategoryController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(long id, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
