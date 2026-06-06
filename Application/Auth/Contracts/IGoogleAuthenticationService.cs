namespace Application.Auth.Contracts;

public interface IGoogleAuthenticationService
{
    Task<GoogleProfile?> AuthenticateAsync(CancellationToken ct);
}

public sealed record GoogleProfile(
    string Email,
    string FirstName,
    string LastName,
    string ProviderKey);