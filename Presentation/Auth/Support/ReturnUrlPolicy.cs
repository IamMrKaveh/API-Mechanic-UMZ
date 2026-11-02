namespace Presentation.Auth.Support;

/// <summary>
/// Keeps the post-login redirect target on this site.
/// </summary>
/// <remarks>
/// The return URL arrives from the query string, so it is attacker-controlled.
/// Only a site-relative path is allowed: anything absolute, protocol-relative
/// (<c>//host</c>) or containing control characters falls back to the root.
/// </remarks>
public static class ReturnUrlPolicy
{
    private const string Fallback = "/";

    public static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Fallback;

        // Must start with exactly one slash: "/path" is relative, "//host" and
        // "/\host" are treated by browsers as protocol-relative absolute URLs.
        if (value[0] != '/')
            return Fallback;

        if (value.Length > 1 && (value[1] == '/' || value[1] == '\\'))
            return Fallback;

        // Control characters can be used to smuggle a new scheme or header.
        if (value.Any(char.IsControl))
            return Fallback;

        return value;
    }
}
