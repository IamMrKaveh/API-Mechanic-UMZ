using Presentation.Common.Configuration;

namespace Tests.Presentation.Common.Configuration;

// Serialized with the other suite that mutates the process working directory.
[Collection("DBContextFactorySequential")]
public class DotEnvLoaderTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "dotenv-loader-tests", Guid.NewGuid().ToString("N"));

    private readonly List<string> _keysToClean = [];

    public DotEnvLoaderTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        foreach (var key in _keysToClean)
            Environment.SetEnvironmentVariable(key, null);

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);

        GC.SuppressFinalize(this);
    }

    private string WriteEnvFile(string content)
    {
        var path = Path.Combine(_directory, ".env");
        File.WriteAllText(path, content);
        return path;
    }

    private string TrackKey(string key)
    {
        _keysToClean.Add(key);
        return key;
    }

    [Fact]
    public void Load_MissingFile_DoesNotThrow()
    {
        var missing = Path.Combine(_directory, "does-not-exist.env");

        Should.NotThrow(() => DotEnvLoader.Load(missing));
    }

    [Fact]
    public void Load_ParsesSimpleKeyValuePairs()
    {
        var path = WriteEnvFile(string.Join('\n',
            "Jwt__Key=super-secret-key-value",
            "Kavenegar__ApiKey=abc123"));

        DotEnvLoader.Load(path);

        Environment.GetEnvironmentVariable(TrackKey("Jwt__Key"))
            .ShouldBe("super-secret-key-value");
        Environment.GetEnvironmentVariable(TrackKey("Kavenegar__ApiKey"))
            .ShouldBe("abc123");
    }

    [Fact]
    public void Load_IgnoCommentsBlankAndMalformedLines()
    {
        var path = WriteEnvFile(string.Join('\n',
            "# a comment",
            "",
            "   ",
            "NO_EQUALS_SIGN_HERE",
            "=value-without-key",
            "  Spaced__Key  =  spaced value  "));

        DotEnvLoader.Load(path);

        Environment.GetEnvironmentVariable(TrackKey("NO_EQUALS_SIGN_HERE")).ShouldBeNull();
        Environment.GetEnvironmentVariable(TrackKey("Spaced__Key"))
            .ShouldBe("spaced value");
    }

    [Fact]
    public void Load_StripsExportPrefix()
    {
        var path = WriteEnvFile("export Exported__Key=exported value");

        DotEnvLoader.Load(path);

        Environment.GetEnvironmentVariable(TrackKey("Exported__Key"))
            .ShouldBe("exported value");
    }

    [Fact]
    public void Load_StripsMatchingQuotes()
    {
        var path = WriteEnvFile(string.Join('\n',
            "Double__Quoted=\"quoted value\"",
            "Single__Quoted='single value'"));

        DotEnvLoader.Load(path);

        Environment.GetEnvironmentVariable(TrackKey("Double__Quoted"))
            .ShouldBe("quoted value");
        Environment.GetEnvironmentVariable(TrackKey("Single__Quoted"))
            .ShouldBe("single value");
    }

    [Fact]
    public void Load_PreservesValueContainingEqualsAndSemicolons()
    {
        // Connection strings contain '=' and ';' - only the first '=' splits.
        const string connection =
            "Host=localhost;Port=5432;Database=ledka;***SEC-01-REMOVED***;SSL Mode=Require";
        var path = WriteEnvFile($"ConnectionStrings__DefaultConnection={connection}");

        DotEnvLoader.Load(path);

        Environment.GetEnvironmentVariable(TrackKey("ConnectionStrings__DefaultConnection"))
            .ShouldBe(connection);
    }

    [Fact]
    public void LoadFromNearestAncestor_FindsEnvFileInParentDirectory()
    {
        // Simulates running the app from a project subdirectory (e.g. Presentation/).
        var repoRoot = Path.Combine(_directory, "repo");
        var projectDir = Path.Combine(repoRoot, "Presentation");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(repoRoot, ".env"), "Ancestor__Key=found-in-ancestor");

        var previousCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(projectDir);

            DotEnvLoader.LoadFromNearestAncestor();
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCwd);
        }

        Environment.GetEnvironmentVariable(TrackKey("Ancestor__Key"))
            .ShouldBe("found-in-ancestor");
    }

    [Fact]
    public void Load_DoesNotOverwriteExistingEnvironmentVariable()
    {
        const string key = "PreExisting__Key";
        Environment.SetEnvironmentVariable(key, "from-environment");
        var path = WriteEnvFile($"{key}=from-env-file");

        DotEnvLoader.Load(path);

        // Real environment variables always win over .env values.
        Environment.GetEnvironmentVariable(TrackKey(key))
            .ShouldBe("from-environment");
    }
}
