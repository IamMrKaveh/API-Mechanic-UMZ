using System.Security.Cryptography;
using System.Text;

namespace Domain.Security.Services;

/// <summary>
/// Hashes refresh tokens (SHA-256) so only an irreversible digest is persisted.
/// A leaked database must not yield directly usable session tokens.
/// </summary>
public static class RefreshTokenHasher
{
    public static string Hash(string rawToken)
    {
        ArgumentNullException.ThrowIfNull(rawToken);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Constant-time comparison of a stored hash against a raw token.</summary>
    public static bool Matches(string? storedHash, string rawToken)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(rawToken))
            return false;

        var candidate = Hash(rawToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(candidate),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
