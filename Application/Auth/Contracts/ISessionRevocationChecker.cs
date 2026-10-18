namespace Application.Auth.Contracts;

/// <summary>
/// Answers whether a session (referenced by the JWT <c>sid</c> claim) is still
/// active, so revoked sessions stop working before their access token expires.
/// </summary>
public interface ISessionRevocationChecker
{
    Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken ct = default);
}
