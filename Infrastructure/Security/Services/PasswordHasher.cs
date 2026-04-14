using Application.Security.Interfaces;

namespace Infrastructure.Security.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (string.IsNullOrWhiteSpace(hash)) return false;
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}