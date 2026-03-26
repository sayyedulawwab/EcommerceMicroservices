using Asp.Versioning;
using Catalog.API.Extensions;
using Catalog.Application.Categories;
using Catalog.Application.Categories.GetCategoryById;
using Catalog.Application.Products;
using Catalog.Application.Products.GetProductById;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Products.GetProduct;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class GetProductController() : ControllerBase
{
    [MapToApiVersion(1)]
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
}