<#
.SYNOPSIS
    Runs the test suite on a chosen container engine (Podman by default).

.DESCRIPTION
    Resolves the Docker-compatible endpoint of Podman (named pipe on Windows,
    unix socket on Linux/macOS), exports DOCKER_HOST and, for rootless Podman,
    TESTCONTAINERS_RYUK_DISABLED=true, then invokes `dotnet test`.

    The test assembly also auto-detects Podman (ContainerEngineBootstrap), so a
    plain `dotnet test` works too. This script pins the engine explicitly and is
    what CI uses.

.PARAMETER Engine
    auto (default: Podman when available, otherwise the Docker default),
    podman (fail when Podman is unavailable), or docker (ignore Podman).

.PARAMETER Filter
    Optional xUnit filter, e.g. "FullyQualifiedName~WalletConfigurationTests".

.PARAMETER Configuration
    Build configuration (default Release).

.PARAMETER NoBuild
    Pass --no-build to `dotnet test`.

.PARAMETER Project
    Test project to run (default Tests/Tests.csproj).

.PARAMETER PodmanPath
    Explicit path to the Podman executable (otherwise PODMAN_PATH, PATH or the
    default install directory is used).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/testing/run-tests.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/testing/run-tests.ps1 -Engine podman -Filter "FullyQualifiedName~Wallet"
#>
[CmdletBinding()]
param(
    [ValidateSet('auto', 'podman', 'docker')]
    [string]$Engine = 'auto',
    [string]$Filter,
    [string]$Configuration = 'Release',
    [switch]$NoBuild,
    [string]$Project = 'Tests/Tests.csproj',
    [string]$PodmanPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

function Get-Field {
    param($Object, [string]$Name)

    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($property) { return $property.Value }
    return $null
}

function Resolve-PodmanExecutable {
    param([string]$Configured)

    if ($Configured) {
        if (-not (Test-Path -LiteralPath $Configured)) {
            throw "Podman executable not found: $Configured"
        }
        return (Resolve-Path -LiteralPath $Configured).Path
    }

    $command = Get-Command podman -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = New-Object System.Collections.Generic.List[string]
    if ($env:ProgramFiles) {
        $candidates.Add((Join-Path $env:ProgramFiles 'RedHat/Podman/podman.exe'))
    }
    $candidates.Add('/usr/bin/podman')
    $candidates.Add('/usr/local/bin/podman')
    $candidates.Add('/opt/homebrew/bin/podman')

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $candidate }
    }

    return $null
}

function Invoke-PodmanJson {
    param(
        [string]$Podman,
        [string[]]$Arguments
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $Podman @Arguments 2>$null
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }

    if ($exitCode -ne 0 -or -not $output) { return $null }

    try {
        return (($output | Out-String) | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

function Get-PodmanConnections {
    param([string]$Podman)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = & $Podman system connection list --format '{{json .}}' 2>$null
    }
    finally {
        $ErrorActionPreference = $previous
    }

    $connections = @()
    if (-not $lines) { return $connections }

    foreach ($line in @($lines)) {
        if ("$line".Trim().Length -eq 0) { continue }
        try { $connections += ("$line" | ConvertFrom-Json) } catch { }
    }

    return $connections
}

function Get-PodmanEndpoint {
    param([string]$Podman)

    $isWindowsHost = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT

    $machine = Invoke-PodmanJson -Podman $Podman -Arguments @('machine', 'inspect', '--format', '{{json .}}')
    $hostInfo = Invoke-PodmanJson -Podman $Podman -Arguments @('info', '--format', '{{json .Host}}')

    $connections = @(Get-PodmanConnections -Podman $Podman)
    $connection = $connections | Where-Object { (Get-Field $_ 'Default') -eq $true } | Select-Object -First 1
    if (-not $connection -and $connections.Count -gt 0) { $connection = $connections[0] }

    $state = Get-Field $machine 'State'
    $isRunning = $state -eq 'running'

    $rootful = Get-Field $machine 'Rootful'
    $rootless = $true
    if ($null -ne $rootful) {
        $rootless = -not $rootful
    }
    else {
        $security = Get-Field $hostInfo 'security'
        $reportedRootless = Get-Field $security 'rootless'
        if ($null -ne $reportedRootless) { $rootless = $reportedRootless }
    }

    if ($isWindowsHost) {
        $pipe = $null
        if ($isRunning) {
            $pipe = Get-Field (Get-Field (Get-Field $machine 'ConnectionInfo') 'PodmanPipe') 'Path'
        }

        if ($pipe) {
            $name = ($pipe -split '\\')[-1]
            return [pscustomobject]@{
                Endpoint = "npipe://./pipe/$name"
                Rootless = $rootless
                Source = "Podman machine '$(Get-Field $machine 'Name')'"
            }
        }

        if ($hostInfo) {
            $name = 'podman-machine-default'
            if (Get-Field $connection 'Name') { $name = Get-Field $connection 'Name' }
            elseif (Get-Field $machine 'Name') { $name = Get-Field $machine 'Name' }

            return [pscustomobject]@{
                Endpoint = "npipe://./pipe/$name"
                Rootless = $rootless
                Source = "Podman machine '$name' (from podman system connection)"
            }
        }

        return $null
    }

    if ($isRunning) {
        $socket = Get-Field (Get-Field (Get-Field $machine 'ConnectionInfo') 'PodmanSocket') 'Path'
        if ($socket) {
            return [pscustomobject]@{
                Endpoint = "unix://$socket"
                Rootless = $rootless
                Source = "Podman machine '$(Get-Field $machine 'Name')'"
            }
        }
    }

    $remoteSocket = Get-Field (Get-Field $hostInfo 'remoteSocket') 'path'
    if ($remoteSocket) {
        return [pscustomobject]@{ Endpoint = $remoteSocket; Rootless = $rootless; Source = 'Podman host socket' }
    }

    $connectionUri = Get-Field $connection 'URI'
    if ($connectionUri -and ($connectionUri -like 'unix://*' -or $connectionUri -like 'tcp://*')) {
        return [pscustomobject]@{
            Endpoint = $connectionUri
            Rootless = $rootless
            Source = "Podman connection '$(Get-Field $connection 'Name')'"
        }
    }

    return $null
}

if ($Engine -eq 'docker') {
    $env:MECHANIC_CONTAINER_ENGINE = 'docker'
    Write-Host 'Container engine: Docker/default (Podman disabled for this run).'
}
else {
    $configuredPodman = if ($PodmanPath) { $PodmanPath } else { $env:PODMAN_PATH }
    $podman = Resolve-PodmanExecutable -Configured $configuredPodman

    if (-not $podman) {
        if ($Engine -eq 'podman') {
            throw 'Podman was requested (-Engine podman) but no Podman executable was found. Install Podman or pass -PodmanPath.'
        }
        Write-Host 'Container engine: Podman not found, using the Docker/default endpoint.'
    }
    else {
        $endpoint = Get-PodmanEndpoint -Podman $podman
        if (-not $endpoint) {
            if ($Engine -eq 'podman') {
                throw "Podman is installed ($podman) but no running machine/socket was found. Start it with: podman machine start"
            }
            Write-Host "Container engine: Podman at $podman has no reachable endpoint, using the Docker/default endpoint."
        }
        else {
            $env:DOCKER_HOST = $endpoint.Endpoint
            if ($endpoint.Rootless -and -not $env:TESTCONTAINERS_RYUK_DISABLED) {
                $env:TESTCONTAINERS_RYUK_DISABLED = 'true'
            }

            $mode = if ($endpoint.Rootless) { 'rootless' } else { 'rootful' }
            Write-Host "Container engine: $($endpoint.Source) via $($endpoint.Endpoint) ($mode)"
        }
    }
}

$testArguments = @('test', $Project, '-c', $Configuration)
if ($NoBuild) { $testArguments += '--no-build' }
if ($Filter) { $testArguments += @('--filter', $Filter) }
$testArguments += @('--logger', 'console;verbosity=minimal')

$previous = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    & dotnet @testArguments
    $exitCode = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $previous
}

exit $exitCode
