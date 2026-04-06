namespace Application.Auth.Contracts;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Domain.User.Aggregates.User user);

    (string AccessToken, string RefreshToken) GenerateTokens(Domain.User.Aggregates.User user);
}