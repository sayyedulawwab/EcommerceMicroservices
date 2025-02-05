﻿using Catalog.API.Extensions;
using Catalog.Application.Products.EditProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Products.EditProduct;
[Route("api/products")]
[ApiController]
public class EditProductController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(long id, EditProductRequest request, CancellationToken cancellationToken)
    {
        var command = new EditProductCommand(id, request.Name, request.Description, request.PriceCurrency, request.PriceAmount,
            request.Quantity, request.CategoryId);

        Result<long> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
