using Domain.User.ValueObjects;

namespace Application.Auth.Contracts;

public interface ISessionService
{
    Task RevokeAllUserSessionsAsync(
        UserId userId,
        CancellationToken ct = default);
}