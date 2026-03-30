using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories;
using Catalog.Application.Categories.AddCategory;
using Catalog.Application.Categories.DeleteCategory;
using Catalog.Application.Categories.EditCategory;
using Catalog.Application.Categories.GetAllCategories;
using Catalog.Application.Categories.GetCategoryById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Categories;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
public class CategoriesController : BaseController
{
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

    [AllowAnonymous]
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(long id, ICommandHandler<DeleteCategoryCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

}
