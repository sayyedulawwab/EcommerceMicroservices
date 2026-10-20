using SharedKernel.Messaging;

namespace Identity.Application.Users.ResendEmailConfirmation;

public record ResendEmailConfirmationCommand(string Email) : ICommand;
