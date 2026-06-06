using Application.Auth.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace Presentation.Auth.Services;

public sealed class HttpGoogleAuthenticationService(
    IHttpContextAccessor httpContextAccessor) : IGoogleAuthenticationService
{
    public async Task<GoogleProfile?> AuthenticateAsync(CancellationToken ct)
    {
        var httpContext = httpContextAccessor.HttpContext!;
        var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return null;

        var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var firstName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        var lastName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        var providerKey = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
            return null;

        return new GoogleProfile(
            email,
            firstName ?? string.Empty,
            lastName ?? string.Empty,
            providerKey);
    }
}