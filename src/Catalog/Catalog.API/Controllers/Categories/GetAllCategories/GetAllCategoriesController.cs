﻿using Catalog.API.Extensions;
using Catalog.Application.Categories;
using Catalog.Application.Categories.GetAllCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Catalog.API.Controllers.Categories.GetAllCategories;
[Route("api/categories")]
[ApiController]
public class GetAllCategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetAllCategoriesQuery();

        Result<IReadOnlyList<CategoryResponse>> result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(result.Value);
    }
}