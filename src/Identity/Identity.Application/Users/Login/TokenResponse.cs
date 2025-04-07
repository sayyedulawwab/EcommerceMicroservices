namespace Identity.Application.Users.Login;
public sealed record TokenResponse(string AccessToken, string RefreshToken);