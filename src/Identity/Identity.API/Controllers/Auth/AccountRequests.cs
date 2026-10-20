namespace Identity.API.Controllers.Auth;

public record ConfirmEmailRequest(string Email, string Token);

public record EmailRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);
