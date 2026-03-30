using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Products;
using Catalog.Application.Products.AddProduct;
using Catalog.Application.Products.DeleteProduct;
using Catalog.Application.Products.EditProduct;
using Catalog.Application.Products.GetProductById;
using Catalog.Application.Products.SearchProduct;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Products;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
public class ProductsController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, ICommandHandler<AddProductCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(
            request.Name,
            request.Description,
            request.PriceCurrency,
            request.PriceAmount,
            request.Quantity,
            request.CategoryId);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(long id, IQueryHandler<GetProductByIdQuery, ProductResponse> handler, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        Result<ProductResponse> result = await handler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(long id, EditProductRequest request, ICommandHandler<EditProductCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.Name, request.Description, request.PriceCurrency, request.PriceAmount,
            request.Quantity, request.CategoryId);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(long id, ICommandHandler<DeleteProductCommand, long> handler, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        Result<long> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductRequest request, IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.CategoryId, request.MinPrice,
            request.MaxPrice, request.Keyword, request.Page, request.PageSize, request.SortColumn,
            request.SortOrder);

        Result<PagedList<ProductResponse>> result = await handler.Handle(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
