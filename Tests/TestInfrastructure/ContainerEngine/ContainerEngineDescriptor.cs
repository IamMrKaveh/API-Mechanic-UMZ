namespace Tests.TestInfrastructure.ContainerEngine;

/// <summary>Container engine used by the database-backed tests.</summary>
internal enum ContainerEngineKind
{
    Docker,
    Podman,
}

/// <summary>
/// A resolved container endpoint for Testcontainers: the value to put in
/// <c>DOCKER_HOST</c> plus the Ryuk decision for rootless Podman.
/// </summary>
internal sealed record ContainerEngineDescriptor(
    ContainerEngineKind Kind,
    string DockerHost,
    bool IsRootless,
    bool DisableRyuk,
    string Description);
