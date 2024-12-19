using Catalog.API.Extensions;
using Catalog.Application.Products.AddProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Products.AddProduct;
[Route("api/products")]
[ApiController]
public class AddProductController : ControllerBase
{
    private readonly ISender _sender;

    public AddProductController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(request.name, request.description, request.priceCurrency, request.priceAmount,
            request.quantity, request.categoryId);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return CreatedAtAction(nameof(GetProduct.GetProductController.GetProduct), new { id = result.Value }, result.Value);
    }
}
