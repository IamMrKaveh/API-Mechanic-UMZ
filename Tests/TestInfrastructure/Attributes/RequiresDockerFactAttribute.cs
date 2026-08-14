using System.Diagnostics;

namespace Tests.TestInfrastructure.Attributes;

public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable.Value)
            Skip = "Docker is not available on this machine. Skipping integration test.";
    }
}

public sealed class RequiresDockerTheoryAttribute : TheoryAttribute
{
    public RequiresDockerTheoryAttribute()
    {
        if (!DockerAvailability.IsAvailable.Value)
            Skip = "Docker is not available on this machine. Skipping integration test.";
    }
}

internal static class DockerAvailability
{
    public static readonly Lazy<bool> IsAvailable = new(Probe, isThreadSafe: true);

    private static bool Probe()
    {
        try
        {
            var psi = new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null) return false;
            if (!process.WaitForExit(3000))
            {
                try { process.Kill(); } catch { /* ignore */ }
                return false;
            }
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
