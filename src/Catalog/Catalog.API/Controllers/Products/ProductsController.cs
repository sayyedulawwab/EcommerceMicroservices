using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Products;
using Catalog.Application.Products.AddProduct;
using Catalog.Application.Products.DeleteProduct;
using Catalog.Application.Products.EditProduct;
using Catalog.Application.Products.GetProductById;
using Catalog.Application.Products.SearchProduct;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Auth;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Products;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
public class ProductsController : BaseController
{
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, ICommandHandler<AddProductCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(
            request.Name,
            request.Description,
            request.PriceCurrency,
            request.PriceAmount,
            request.Quantity,
            request.CategoryId);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created(string.Empty, new { id = result.Value });
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, IQueryHandler<GetProductByIdQuery, ProductResponse> handler, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        Result<ProductResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditProduct(Guid id, EditProductRequest request, ICommandHandler<EditProductCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.Name, request.Description, request.PriceCurrency, request.PriceAmount,
            request.Quantity, request.CategoryId);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, ICommandHandler<DeleteProductCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductRequest request, IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.CategoryId, request.MinPrice,
            request.MaxPrice, request.Keyword, request.Page, request.PageSize, request.SortColumn,
            request.SortOrder);

        Result<PagedList<ProductResponse>> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
