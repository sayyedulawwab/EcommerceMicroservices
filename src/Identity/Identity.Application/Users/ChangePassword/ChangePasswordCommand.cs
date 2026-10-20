using SharedKernel.Messaging;

namespace Identity.Application.Users.ChangePassword;

public record ChangePasswordCommand(long UserId, string CurrentPassword, string NewPassword) : ICommand<AuthTokens>;
