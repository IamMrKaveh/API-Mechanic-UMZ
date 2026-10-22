using System.Text.Json;
using SharedContracts.FeatureManagement;

namespace Tests.Architecture;

/// <summary>
/// The defaults that protect money and orders have to be on in the committed
/// configuration. The feature flags stay in the code as a kill switch, so a
/// deployment can turn them off on purpose, but the configuration that ships
/// with the repository must not turn them off by accident.
/// </summary>
public class SafeDefaultsTests
{
    [Theory]
    [InlineData(FeatureFlags.IdempotencyDistributedLockEnabled)]
    [InlineData(FeatureFlags.SagaAutoRefundOnCommitFailure)]
    public void CommittedConfiguration_KeepsTheSafeDefaultsOn(string flag)
    {
        var settings = ReadSettings("appsettings.json");
        var section = settings.GetProperty("FeatureManagement");
        var value = section.GetProperty(flag).GetBoolean();

        value.ShouldBeTrue(
            $"FeatureManagement:{flag} must be on in appsettings.json. The flag is a kill switch, " +
            "so it may only be turned off on purpose.");
    }

    private static JsonElement ReadSettings(string fileName)
    {
        var path = Path.Combine(RepositoryRoot(), "Presentation", fileName);
        var json = File.ReadAllText(path);

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        return document.RootElement.Clone();
    }

    private static string RepositoryRoot()
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
}
