using SharedKernel.Messaging;

namespace Identity.Application.Users.ConfirmEmail;

public record ConfirmEmailCommand(string Email, string Token) : ICommand;
