using Catalog.API.Extensions;
using Catalog.Application.Products.AddProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

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
        var command = new AddProductCommand(request.Name, request.Description, request.PriceCurrency, request.PriceAmount,
            request.Quantity, request.CategoryId);

        Result<long> result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }
}
