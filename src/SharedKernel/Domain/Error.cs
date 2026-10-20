using System.Net;

namespace SharedKernel.Domain;

public record Error(string Name, string Description, HttpStatusCode Code)
{
    public static readonly Error None = new(string.Empty, string.Empty, HttpStatusCode.InternalServerError);

    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided", HttpStatusCode.BadRequest);

    public static Error InternalServerError(string name, string description) =>
       new(name, description, HttpStatusCode.InternalServerError);

    public static Error NotFound(string name, string description) =>
        new(name, description, HttpStatusCode.NotFound);

    public static Error BadRequest(string name, string description) =>
        new(name, description, HttpStatusCode.BadRequest);

    public static Error Conflict(string name, string description) =>
        new(name, description, HttpStatusCode.Conflict);

    public static Error Unauthorized(string name, string description) =>
        new(name, description, HttpStatusCode.Unauthorized);

    public static Error Forbidden(string name, string description) =>
        new(name, description, HttpStatusCode.Forbidden);
}
