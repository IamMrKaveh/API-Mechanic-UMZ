using System.Text.RegularExpressions;

namespace Tests.Architecture;

/// <summary>
/// Guards the error semantics of the Application layer.
///
/// A handler that catches a domain exception and returns a generic failure
/// downgrades a business-rule violation to an opaque error, and a handler that
/// rebuilds an error from its message alone throws away the type that decides
/// the HTTP status code.
/// </summary>
public class ErrorTypeConsistencyTests
{
    private static readonly string[] ProductionProjects =
        ["Application", "Domain", "Infrastructure", "Presentation", "SharedKernel", "SharedContracts"];

    private static IEnumerable<(string File, string Text)> ReadProductionSources()
    {
        var testDirectory = Path.GetDirectoryName(typeof(ErrorTypeConsistencyTests).Assembly.Location)!;
        var repositoryRoot = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", ".."));

        foreach (var project in ProductionProjects)
        {
            var projectDirectory = Path.Combine(repositoryRoot, project);

            if (!Directory.Exists(projectDirectory))
                continue;

            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                yield return (file, File.ReadAllText(file));
            }
        }
    }

    [Fact]
    public void Handlers_DoNotRebuildAnErrorFromItsMessageAlone()
    {
        var offenders = new List<string>();

        foreach (var (file, text) in ReadProductionSources())
        {
            if (Regex.IsMatch(text, @"\.Failure\(\s*\w+[.\w]*\.Error\.Message\s*\)"))
                offenders.Add(Path.GetFileName(file));
        }

        offenders.ShouldBeEmpty(
            "An error must keep its type: use Failure(error) instead of Failure(error.Message).");
    }

    [Fact]
    public void DomainExceptionsCaughtByHandlers_AreReportedAsBusinessRuleViolations()
    {
        var offenders = new List<string>();
        var pattern = new Regex(
            @"catch\s*\(\s*(?:SharedKernel\.Exceptions\.)?DomainException\s+\w+\s*\)\s*\{[^}]*?\.Failure\(\s*\w+\.Message\s*\)",
            RegexOptions.Singleline);

        foreach (var (file, text) in ReadProductionSources())
        {
            if (pattern.IsMatch(text))
                offenders.Add(Path.GetFileName(file));
        }

        offenders.ShouldBeEmpty(
            "A domain exception is a business-rule violation: report it as ErrorType.BusinessRule.");
    }
}
