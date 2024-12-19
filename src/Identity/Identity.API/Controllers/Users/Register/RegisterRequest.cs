namespace Identity.API.Controllers.Users.Register;

public record RegisterRequest(string firstName, string lastName, string email, string password);