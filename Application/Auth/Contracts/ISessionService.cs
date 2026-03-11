namespace Application.Auth.Contracts;

public interface ISessionService
{
    Task RevokeAllUserSessionsAsync(
        int userId,
        CancellationToken ct = default);
}