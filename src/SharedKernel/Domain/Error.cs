namespace SharedKernel.Domain;

public record Error(string Name, string Description, HttpResponseStatusCodes Code)
{
    public static readonly Error None = new(string.Empty, string.Empty, HttpResponseStatusCodes.InternalServerError);

    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided", HttpResponseStatusCodes.BadRequest);

    public static Error InternalServerError(string name, string description) =>
       new(name, description, HttpResponseStatusCodes.InternalServerError);

    public static Error NotFound(string name, string description) =>
        new(name, description, HttpResponseStatusCodes.NotFound);

    public static Error BadRequest(string name, string description) =>
        new(name, description, HttpResponseStatusCodes.BadRequest);

    public static Error Conflict(string name, string description) =>
        new(name, description, HttpResponseStatusCodes.Conflict);
}