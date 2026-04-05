using Domain.User.Aggregates;

namespace Application.Auth.Contracts;

public interface IJwtTokenGenerator
{
    (string AccessToken, string RefreshToken) GenerateTokens(User user);
}