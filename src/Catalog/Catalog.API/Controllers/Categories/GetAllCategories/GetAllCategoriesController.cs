using Catalog.API.Extensions;
using Catalog.Application.Categories.GetAllCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Categories.GetAllCategories;
[Route("api/categories")]
[ApiController]
public class GetAllCategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public GetAllCategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllCategoriesQuery();

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
