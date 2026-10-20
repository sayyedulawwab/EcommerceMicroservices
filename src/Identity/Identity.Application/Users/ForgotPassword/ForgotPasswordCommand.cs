using SharedKernel.Messaging;

namespace Identity.Application.Users.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommand;
