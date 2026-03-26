using Identity.Application.Abstractions.Auth;
using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.Login;
internal sealed class LoginUserQueryHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtService jwtService)
    : IQueryHandler<LoginUserQuery, TokenResponse>
{
    public async Task<Result<TokenResponse>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {

        User? user = await userRepository.GetByEmail(request.Email);

        if (user is null)
        {
            return Result.Failure<TokenResponse>(UserErrors.NotFound);
        }

        bool isPasswordValid = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            return Result.Failure<TokenResponse>(UserErrors.InvalidCredentials);
        }

        string accessToken = jwtService.GetAccessToken(
            request.Email,
            user.Id,
            cancellationToken);

        var refreshToken = RefreshToken.Create(jwtService.GenerateRefreshToken(), user.Id, DateTime.UtcNow.AddDays(7));

        refreshTokenRepository.Add(refreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenResponse(accessToken, refreshToken.Token);
    }

}