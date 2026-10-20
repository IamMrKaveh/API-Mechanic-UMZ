using System.Text.Json;

namespace Tests.Security;

/// <summary>
/// Shared helpers for the phase-6 security invariant tests
/// (see docs/07-operations/security-invariants.md). The invariants protect the
/// strengths listed in phase 6 of the security audit against silent regressions.
/// </summary>
internal static class SecurityInvariantHelpers
{
    private static readonly string[] ProductionProjects =
        ["Application", "Domain", "Infrastructure", "Presentation", "SharedKernel", "SharedContracts"];

    private static readonly Lazy<IReadOnlyList<SourceLine>> SourceLines =
        new(() => LoadProductionSourceLines().ToList(), isThreadSafe: true);

    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "API.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root (API.sln) above '{AppContext.BaseDirectory}'.");
    }

    /// <summary>All non-comment-free source lines of the production projects.</summary>
    internal static IReadOnlyList<SourceLine> ProductionSource => SourceLines.Value;

    internal static JsonElement ReadAppSettings()
    {
        var path = Path.Combine(RepositoryRoot(), "Presentation", "appsettings.json");
        var json = File.ReadAllText(path);

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        return document.RootElement.Clone();
    }

    internal static JsonElement GetSection(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var name in path)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !TryGetProperty(current, name, out var next))
            {
                throw new InvalidOperationException(
                    $"Configuration section '{string.Join(':', path)}' is missing from appsettings.json.");
            }

            current = next;
        }

        return current;
    }

    internal static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    internal static bool GetBoolean(JsonElement root, params string[] path)
        => GetSection(root, path).GetBoolean();

    internal static int GetInt32(JsonElement root, params string[] path)
        => GetSection(root, path).GetInt32();

    internal static string? GetString(JsonElement root, params string[] path)
    {
        var value = GetSection(root, path);
        return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
    }

    internal static IReadOnlyList<string> GetStringArray(JsonElement root, params string[] path)
        => GetSection(root, path)
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();

    /// <summary>Removes a trailing line comment so documentation comments do not trip the scan.</summary>
    internal static string WithoutComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return index >= 0 ? line[..index] : line;
    }

    internal static IReadOnlyList<string> FindViolations(
        IReadOnlyList<SourceLine> lines,
        IEnumerable<string> appendPatterns)
    {
        var patterns = string.Join("|", appendPatterns.Select(fragment => fragment));
        var violations = new List<string>();

        foreach (var line in lines)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(line.Text, patterns,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                violations.Add($"{line.File}:{line.Line}: {line.Text.Trim()}");
            }
        }

        return violations;
    }

    private static IEnumerable<SourceLine> LoadProductionSourceLines()
    {
        var root = RepositoryRoot();

        foreach (var project in ProductionProjects)
        {
            var projectDirectory = Path.Combine(root, project);
            if (!Directory.Exists(projectDirectory))
                continue;

            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                    relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var lineNumber = 0;
                foreach (var text in File.ReadLines(file))
                {
                    lineNumber++;
                    yield return new SourceLine(relative, lineNumber, text);
                }
            }
        }
    }

    internal sealed record SourceLine(string File, int Line, string Text);
}
