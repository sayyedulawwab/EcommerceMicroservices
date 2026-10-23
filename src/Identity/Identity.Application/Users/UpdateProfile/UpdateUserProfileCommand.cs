using SharedKernel.Messaging;

namespace Identity.Application.Users.UpdateProfile;

public record UpdateUserProfileCommand(Guid UserId, string FirstName, string LastName) : ICommand;
