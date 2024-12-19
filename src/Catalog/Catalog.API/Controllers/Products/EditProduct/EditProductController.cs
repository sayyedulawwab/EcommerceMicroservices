using Catalog.API.Extensions;
using Catalog.Application.Products.EditProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Products.EditProduct;
[Route("api/products")]
[ApiController]
public class EditProductController : ControllerBase
{
    private readonly ISender _sender;

    public EditProductController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(long id, EditProductRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.name, request.description, request.priceCurrency, request.priceAmount,
            request.quantity, request.categoryId);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
