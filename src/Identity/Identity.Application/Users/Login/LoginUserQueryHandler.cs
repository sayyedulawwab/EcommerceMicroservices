using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Messaging;
using Identity.Domain.Abstractions;
using Identity.Domain.Users;

namespace Identity.Application.Users.Login;
internal sealed class LoginUserQueryHandler : IQueryHandler<LoginUserQuery, AccessTokenResponse>
{

    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    public LoginUserQueryHandler(IUserRepository userRepository, IAuthService authService, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _authService = authService;
        _jwtService = jwtService;
    }

    public async Task<Result<AccessTokenResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {

        var user = await _userRepository.GetByEmail(request.email);

        if (user is null)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);
        }

        var hashedPassword = _authService.HashPassword(request.password, user.PasswordSalt);

        if (hashedPassword != user.PasswordHash)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }


        var result = _jwtService.GetAccessToken(
            request.email,
            user.Id,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }

        return new AccessTokenResponse(result.Value);
    }

}
