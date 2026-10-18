namespace Presentation.Common.Configuration;

/// <summary>
/// Minimal loader for <c>.env</c> files (one <c>KEY=VALUE</c> per line) into
/// process environment variables. Secrets live in a gitignored <c>.env</c>
/// instead of tracked <c>appsettings*.json</c> files.
/// </summary>
/// <remarks>
/// Rules: blank lines and lines starting with <c>#</c> are ignored; an optional
/// <c>export </c> prefix is stripped; the value is everything after the first
/// <c>=</c> (so connection strings containing <c>=</c> are preserved); matching
/// surrounding single or double quotes are removed. An existing environment
/// variable is never overwritten, so container/CI secrets always win.
/// A missing file is a no-op.
/// </remarks>
public static class DotEnvLoader
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line["export ".Length..].TrimStart();

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            if (key.Length == 0)
                continue;

            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            // Real environment variables take precedence over .env values.
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    /// <summary>
    /// Loads the nearest <c>.env</c> starting at the current directory and
    /// walking up the directory tree (repo-root discovery). Useful when the
    /// app runs from a project subdirectory (e.g. <c>Presentation/</c>).
    /// </summary>
    public static void LoadFromNearestAncestor()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                Load(candidate);
                return;
            }

            directory = directory.Parent;
        }
    }
}
