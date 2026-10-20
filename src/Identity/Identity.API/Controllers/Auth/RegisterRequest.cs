namespace Identity.API.Controllers.Auth;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);