namespace Application.Common.Extensions;

public static class RowVersionExtensions
{
    public static string? ToBase64(this byte[]? rowVersion)
        => rowVersion is null ? null : Convert.ToBase64String(rowVersion);

    public static byte[]? FromBase64RowVersion(this string? base64)
        => string.IsNullOrWhiteSpace(base64) ? null : Convert.FromBase64String(base64);
}