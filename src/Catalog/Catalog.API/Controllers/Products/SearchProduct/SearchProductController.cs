using Asp.Versioning;
using Catalog.Application.Products;
using Catalog.Application.Products.SearchProduct;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Products.SearchProduct;
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class SearchProductController(ISender sender) : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.CategoryId, request.MinPrice,
            request.MaxPrice, request.Keyword, request.Page, request.PageSize, request.SortColumn,
            request.SortOrder);

        Result<PagedList<ProductResponse>> result = await sender.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}