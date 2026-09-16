namespace Presentation.Common.Options;

public sealed class AuthCookieOptions
{
    public const string SectionName = "Auth:Cookie";

    public string RefreshTokenName { get; init; } = "__Host-refresh";

    public string Path { get; init; } = "/";

    public string SameSite { get; init; } = "Strict";

    public bool Secure { get; init; } = true;

    public bool HttpOnly { get; init; } = true;

    public string? Domain { get; init; }
}
