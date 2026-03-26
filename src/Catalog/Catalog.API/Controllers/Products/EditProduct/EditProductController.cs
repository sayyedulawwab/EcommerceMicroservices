using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Products.DeleteProduct;
using Catalog.Application.Products.EditProduct;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Products.EditProduct;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class EditProductController() : ControllerBase
{
    [MapToApiVersion(1)]
    [Authorize]
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
}
