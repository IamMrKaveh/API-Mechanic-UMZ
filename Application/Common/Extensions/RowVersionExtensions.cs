namespace Application.Common.Extensions;

public static class RowVersionExtensions
{
    public static string? ToBase64(this byte[]? rowVersion)
        => rowVersion is null ? null : Convert.ToBase64String(rowVersion);
}