using System.Globalization;
using System.Security.Claims;

namespace Cart.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long? GetUserId(this ClaimsPrincipal? principal)
    {
        string? value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long userId)
            ? userId
            : null;
    }
}
