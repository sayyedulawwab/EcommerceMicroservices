using SharedKernel.Messaging;

namespace Identity.Application.Users.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand;
