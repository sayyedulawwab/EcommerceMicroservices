namespace Identity.API.Controllers.Users.Register;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);