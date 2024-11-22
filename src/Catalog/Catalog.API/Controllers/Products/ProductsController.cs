using Catalog.Application.Products.Commands.AddProduct;
using Catalog.Application.Products.Commands.DeleteProduct;
using Catalog.Application.Products.Commands.EditProduct;
using Catalog.Application.Products.Queries.GetProductById;
using Catalog.Application.Products.Queries.SearchProduct;
using Catalog.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Products;

[Route("api/products")]
[ApiController]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    public ProductsController(ISender sender)
    {
        _sender = sender;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.categoryId, request.minPrice, 
            request.maxPrice, request.keyword, request.page, request.pageSize, request.sortColumn, 
            request.sortOrder);

        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(long id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(request.name, request.description, request.priceCurrency, request.priceAmount, 
            request.quantity, request.categoryId);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetProduct), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(long id, EditProductRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.name, request.description, request.priceCurrency, request.priceAmount,
            request.quantity, request.categoryId);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.type == HttpResponseStatusCodes.NotFound)
            {
                return NotFound(result.Error);
            }

            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.type == HttpResponseStatusCodes.NotFound)
            {
                return NotFound(result.Error);
            }

            return BadRequest(result.Error);
        }

        return Ok(new { id = result.Value });
    }
}
