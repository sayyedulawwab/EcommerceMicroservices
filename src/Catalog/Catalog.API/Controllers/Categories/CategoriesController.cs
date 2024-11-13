using Catalog.Application.Categories.Commands.AddCategory;
using Catalog.Application.Categories.Commands.DeleteCategory;
using Catalog.Application.Categories.Commands.EditCategory;
using Catalog.Application.Categories.Queries.GetAllCategories;
using Catalog.Application.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Categories;

[Route("api/categories")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ISender _sender;
    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllCategoriesQuery();

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(long id, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCategoryCommand(request.name, request.code);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetCategory), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(long id, EditCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.name, request.code);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }


}
