using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Clock;
using Identity.Application.Abstractions.Messaging;
using Identity.Domain.Users;
using SharedKernel.Domain;

namespace Identity.Application.Users.Register;
internal sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, long>
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;


    public RegisterUserCommandHandler(IPasswordHasher passwordHasher, IUserRepository userRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _passwordHasher = passwordHasher;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }
    public async Task<Result<long>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUserByEmail = await _userRepository.GetByEmail(request.email);

        if (existingUserByEmail is not null)
        {
            return Result.Failure<long>(UserErrors.AlreadyExists);
        }

        var passwordSalt = _passwordHasher.GenerateSalt();
        var hashedPassword = _passwordHasher.Hash(request.password, passwordSalt);

        var user = User.Create(
            request.firstName,
            request.lastName,
            request.email, 
            hashedPassword,
            false,
            _dateTimeProvider.UtcNow);

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync();

        return user.Id;
    }
}
