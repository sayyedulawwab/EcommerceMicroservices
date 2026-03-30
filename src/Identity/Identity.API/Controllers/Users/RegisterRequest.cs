namespace Identity.API.Controllers.Users;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);