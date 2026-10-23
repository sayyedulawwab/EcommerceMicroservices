using System.Security.Claims;

namespace Cart.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? principal)
    {
        string? value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(value, out Guid userId)
            ? userId
            : null;
    }
}
