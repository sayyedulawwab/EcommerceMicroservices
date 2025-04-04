using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Products;
using Catalog.Application.Products.GetProductById;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Products.GetProduct;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class GetProductController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(long id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        Result<ProductResponse> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}