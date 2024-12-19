using Catalog.Application.Products.SearchProduct;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers.Products.SearchProduct;
[Route("api/products")]
[ApiController]
public class SearchProductController : ControllerBase
{
    private readonly ISender _sender;

    public SearchProductController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(request.categoryId, request.minPrice,
            request.maxPrice, request.keyword, request.page, request.pageSize, request.sortColumn,
            request.sortOrder);

        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
