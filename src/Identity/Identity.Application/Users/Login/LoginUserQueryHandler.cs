using Identity.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Messaging;
using Identity.Domain.Users;
using SharedKernel.Domain;

namespace Identity.Application.Users.Login;
internal sealed class LoginUserQueryHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService)
    : IQueryHandler<LoginUserQuery, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {

        User? user = await userRepository.GetByEmail(request.Email);

        if (user is null)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.NotFound);
        }

        bool isPasswordValid = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidCredentials);
        }


        Result<string> result = jwtService.GetAccessToken(
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