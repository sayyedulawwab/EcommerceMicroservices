using SharedKernel.Messaging;

namespace Identity.Application.Users.Logout;

public record LogoutCommand(string? RefreshToken) : ICommand;
