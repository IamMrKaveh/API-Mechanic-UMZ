using Domain.Security.ValueObjects;

namespace Application.Auth.Contracts;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Domain.User.Aggregates.User user, SessionId sessionId);
}