using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Messaging;
using Identity.Domain.Users;
using SharedKernel.Domain;

namespace Identity.Application.Users.Login;
internal sealed class LoginUserQueryHandler : IQueryHandler<LoginUserQuery, AccessTokenResponse>
{

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    public LoginUserQueryHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AccessTokenResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {

        User? user = await _userRepository.GetByEmail(request.Email);

        if (user is null)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);
        }

        bool isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }


        Result<string> result = _jwtService.GetAccessToken(
            request.Email,
            user.Id,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        return new AccessTokenResponse(result.Value);
    }

}