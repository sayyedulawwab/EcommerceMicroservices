using Identity.Application.Abstractions.Auth;
using Identity.Domain.Users;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Identity.Application.Users.Login;
internal sealed class LoginUserWithRefreshTokenQueryHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IJwtService jwtService)
    : IQueryHandler<LoginUserWithRefreshTokenQuery, TokenResponse>
{
    public async Task<Result<TokenResponse>> Handle(LoginUserWithRefreshTokenQuery request, CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (refreshToken is null || refreshToken.ExpiresOnUtc < DateTime.UtcNow)
        {
            return Result.Failure<TokenResponse>(RefreshTokenErrors.Expired);
        }

        string accessToken = jwtService.GetAccessToken(
            refreshToken.User.Email,
            refreshToken.UserId,
            cancellationToken);

        refreshToken = RefreshToken.Update(refreshToken, jwtService.GenerateRefreshToken(), DateTime.UtcNow.AddDays(7));

        refreshTokenRepository.Update(refreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new TokenResponse(accessToken, refreshToken.Token);

        return response;
    }
}