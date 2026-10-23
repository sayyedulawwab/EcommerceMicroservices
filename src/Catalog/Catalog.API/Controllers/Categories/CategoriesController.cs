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
using SharedKernel.Auth;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Categories;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/categories")]
public class CategoriesController : BaseController
{
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> AddCategory(AddCategoryRequest request, ICommandHandler<AddCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new AddCategoryCommand(request.Name, request.Description, request.ParentCategoryId);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, new { id = result.Value });
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
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategory(Guid id, IQueryHandler<GetCategoryByIdQuery, CategoryResponse> handler, CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery(id);

        Result<CategoryResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditCategory(Guid id, EditCategoryRequest request, ICommandHandler<EditCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.Name, request.Description, request.ParentCategoryId);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, ICommandHandler<DeleteCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

}
