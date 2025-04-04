using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Products.AddProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Products.AddProduct;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class AddProductController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductRequest request, CancellationToken cancellationToken)
    {
        var command = new AddProductCommand(
            request.Name,
            request.Description,
            request.PriceCurrency,
            request.PriceAmount,
            request.Quantity,
            request.CategoryId);

        Result<long> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Created();
    }
}