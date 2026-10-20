namespace Identity.API.Controllers.Users;

public record UpdateProfileRequest(string FirstName, string LastName);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
