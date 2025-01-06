using Catalog.API.Extensions;
using Catalog.Application.Products;
using Catalog.Application.Products.GetProductById;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Products.GetProduct;
[Route("api/products")]
[ApiController]
public class GetProductController : ControllerBase
{
    private readonly ISender _sender;

    public GetProductController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(long id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);

        Result<ProductResponse> result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}
