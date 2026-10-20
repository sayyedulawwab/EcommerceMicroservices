using SharedKernel.Messaging;

namespace Identity.Application.Users.UpdateProfile;

public record UpdateUserProfileCommand(long UserId, string FirstName, string LastName) : ICommand;
