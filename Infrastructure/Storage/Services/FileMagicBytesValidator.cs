using Application.Storage.Contracts;

namespace Infrastructure.Storage.Services;

public sealed class FileMagicBytesValidator : IFileMagicBytesValidator
{
    private static readonly IReadOnlyDictionary<string, byte[][]> Signatures =
        new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new[]
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xDB },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xEE }
            },
            ["image/png"] = new[]
            {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            },
            ["image/gif"] = new[]
            {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }
            },
            ["image/webp"] = new[]
            {
                new byte[] { 0x52, 0x49, 0x46, 0x46 }
            }
        };

    public async Task<bool> IsAllowedAsync(Stream stream, string declaredContentType, CancellationToken ct = default)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrWhiteSpace(declaredContentType)) return false;
        if (!Signatures.TryGetValue(declaredContentType, out var candidates)) return false;

        if (!stream.CanSeek) return false;

        var maxLength = candidates.Max(c => c.Length);
        var header = new byte[maxLength];

        stream.Position = 0;
        var read = await stream.ReadAsync(header.AsMemory(0, maxLength), ct);
        stream.Position = 0;

        if (read < candidates.Min(c => c.Length)) return false;

        foreach (var candidate in candidates)
        {
            if (read < candidate.Length) continue;
            var match = true;
            for (var i = 0; i < candidate.Length; i++)
            {
                if (header[i] != candidate[i]) { match = false; break; }
            }
            if (match)
            {
                if (declaredContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
                {
                    if (read < 12) return false;
                    if (header[8] != 0x57 || header[9] != 0x45 || header[10] != 0x42 || header[11] != 0x50)
                        return false;
                }
                return true;
            }
        }

        return false;
    }
}
