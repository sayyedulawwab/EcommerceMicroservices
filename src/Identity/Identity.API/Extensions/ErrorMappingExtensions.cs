using Microsoft.AspNetCore.Mvc;
using SharedKernel.Domain;

namespace Identity.API.Extensions;

public static class ErrorMappingExtensions
{
    public static IActionResult ToActionResult(this Error error)
    {
        return new ObjectResult(error) { StatusCode = (int)error.Code };
    }
}
