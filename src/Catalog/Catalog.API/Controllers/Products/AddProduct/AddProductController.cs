using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Products.AddProduct;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Products.AddProduct;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class AddProductController() : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
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
}