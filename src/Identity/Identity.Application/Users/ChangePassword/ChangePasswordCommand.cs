using SharedKernel.Messaging;

namespace Identity.Application.Users.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<AuthTokens>;
