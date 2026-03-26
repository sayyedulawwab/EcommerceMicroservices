using Asp.Versioning;
using Catalog.Application.Products;
using Catalog.Application.Products.SearchProduct;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.API.Controllers.Products.SearchProduct;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/products")]
[ApiController]
public class SearchProductController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductRequest request, IQueryHandler<SearchProductsQuery, PagedList<ProductResponse>> handler, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.CategoryId, request.MinPrice,
            request.MaxPrice, request.Keyword, request.Page, request.PageSize, request.SortColumn,
            request.SortOrder);

        Result<PagedList<ProductResponse>> result = await handler.Handle(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}