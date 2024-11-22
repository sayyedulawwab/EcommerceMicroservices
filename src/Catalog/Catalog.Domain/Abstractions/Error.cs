namespace Catalog.Domain.Abstractions;

public record Error(string code, string description, HttpResponseStatusCodes type)
{
    public static Error None = new(string.Empty, string.Empty, HttpResponseStatusCodes.InternalServerError);

    public static Error NullValue = new("Error.NullValue", "Null value was provided", HttpResponseStatusCodes.BadRequest);

    public static Error InternalServerError(string code, string description) =>
       new(code, description, HttpResponseStatusCodes.InternalServerError);

    public static Error NotFound(string code, string description) =>
        new(code, description, HttpResponseStatusCodes.NotFound);

    public static Error BadRequest(string code, string description) =>
        new(code, description, HttpResponseStatusCodes.BadRequest);

    public static Error Conflict(string code, string description) =>
        new(code, description, HttpResponseStatusCodes.Conflict);
}