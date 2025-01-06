using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Identity.API.Extensions;

public static class ErrorMappingExtensions
{
    public static IActionResult ToActionResult(this Error error)
    {
        return error.Code switch
        {
            HttpResponseStatusCodes.NotFound => new NotFoundObjectResult(error),
            HttpResponseStatusCodes.BadRequest => new BadRequestObjectResult(error),
            HttpResponseStatusCodes.Conflict => new ConflictObjectResult(error),
            HttpResponseStatusCodes.InternalServerError => new ObjectResult(error)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },
            _ => new ObjectResult(error) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}
