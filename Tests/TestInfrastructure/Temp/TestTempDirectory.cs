namespace Tests.TestInfrastructure.Temp;

/// <summary>
/// Builds disposable temp folders for tests.
/// </summary>
/// <remarks>
/// The vstest test host runs at low integrity on Windows and cannot write to
/// <c>%TEMP%</c> itself (medium mandatory label). <c>%TEMP%\Low</c> stays writable
/// for that process class, so the helper prefers it whenever it exists; the
/// setup script creates it once. High and low integrity processes can both
/// write inside <c>%TEMP%\Low</c>.
/// </remarks>
internal static class TestTempDirectory
{
    /// <summary>Creates a unique leaf folder under <c>[root]/{category}/{guid}</c> and returns it.</summary>
    internal static string Create(string category)
    {
        var directory = Path.Combine(BaseRoot(), category, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Creates (or returns) the <c>[root]/{category}</c> folder.</summary>
    internal static string CreateRoot(string category)
    {
        var directory = Path.Combine(BaseRoot(), category);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string BaseRoot()
    {
        var temp = Path.GetTempPath();
        var low = Path.Combine(temp, "Low");

        return Directory.Exists(low) ? low : temp;
    }
}
