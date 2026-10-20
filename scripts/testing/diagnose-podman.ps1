<#
.SYNOPSIS
    Diagnoses why the database tests cannot reach Podman from this process/session.

.DESCRIPTION
    Prints the Podman installation, machine state, named pipes, reachability of
    each pipe, the Docker-compatible connection list and the relevant environment
    variables, then recommends a fix.

    Run it in the same context as the test runner (for Visual Studio: same
    elevation level and Windows session as Podman Desktop).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/testing/diagnose-podman.ps1
#>
[CmdletBinding()]
param(
    [string]$PodmanPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-Field {
    param($Object, [string]$Name)

    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($property) { return $property.Value }
    return $null
}

function Test-Pipe {
    param(
        [string]$Name,
        [int]$TimeoutMilliseconds = 1500
    )

    try {
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(
            '.', $Name, [System.IO.Pipes.PipeDirection]::InOut)
        try {
            $pipe.Connect($TimeoutMilliseconds)
            return $pipe.IsConnected
        }
        finally {
            $pipe.Dispose()
        }
    }
    catch {
        return $false
    }
}

function Resolve-PodmanExecutable {
    param([string]$Configured)

    if ($Configured -and (Test-Path -LiteralPath $Configured)) {
        return (Resolve-Path -LiteralPath $Configured).Path
    }

    $command = Get-Command podman -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = New-Object System.Collections.Generic.List[string]
    if ($env:ProgramFiles) { $candidates.Add((Join-Path $env:ProgramFiles 'RedHat/Podman/podman.exe')) }
    $candidates.Add('/usr/bin/podman')
    $candidates.Add('/usr/local/bin/podman')

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }

    return $null
}


$isWindowsHost = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
$isElevated = $null
if ($isWindowsHost) {
    $isElevated = ([Security.Principal.WindowsPrincipal]::new(
        [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator))
}

Write-Host '== Podman diagnostics =='
Write-Host ("OS:        {0}" -f [System.Environment]::OSVersion.VersionString)
if ($null -ne $isElevated) {
    Write-Host ("Elevated:  {0}" -f $isElevated)
}
Write-Host ''

$podman = Resolve-PodmanExecutable -Configured $(if ($PodmanPath) { $PodmanPath } else { $env:PODMAN_PATH })
if (-not $podman) {
    Write-Host 'Podman executable not found (PATH, PODMAN_PATH, default install dir).' -ForegroundColor Red
    Write-Host 'Install Podman Desktop/CLI, then run: podman machine init; podman machine start'
    exit 2
}

Write-Host ("Podman:    {0}" -f $podman)
& $podman version 2>&1 | Select-Object -First 4
Write-Host ''

Write-Host '== Machines =='
& $podman machine list 2>&1
$machine = $null
$previous = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    $machineJson = & $podman machine inspect --format '{{json .}}' 2>$null
    if ($LASTEXITCODE -eq 0 -and $machineJson) {
        try { $machine = ($machineJson | Out-String | ConvertFrom-Json) } catch { }
    }
}
finally {
    $ErrorActionPreference = $previous
}

if ($machine) {
    Write-Host ''
    Write-Host ("State:     {0}" -f (Get-Field $machine 'State'))
    Write-Host ("Rootful:   {0}" -f (Get-Field $machine 'Rootful'))
    Write-Host ("Pipe:      {0}" -f (Get-Field (Get-Field (Get-Field $machine 'ConnectionInfo') 'PodmanPipe') 'Path'))
    Write-Host ("Socket:    {0}" -f (Get-Field (Get-Field (Get-Field $machine 'ConnectionInfo') 'PodmanSocket') 'Path'))
}
else {
    Write-Host 'podman machine inspect failed (lockfile/permission or no machine).'
}

Write-Host ''
Write-Host '== podman info =='
$previous = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    $hostJson = & $podman info --format '{{json .Host}}' 2>$null
    $infoExit = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $previous
}

$hostInfo = $null
if ($infoExit -eq 0 -and $hostJson) {
    try { $hostInfo = ($hostJson | Out-String | ConvertFrom-Json) } catch { }
}

if ($hostInfo) {
    Write-Host ("rootless:        {0}" -f (Get-Field (Get-Field $hostInfo 'security') 'rootless'))
    Write-Host ("serviceIsRemote: {0}" -f (Get-Field $hostInfo 'serviceIsRemote'))
    Write-Host ("remoteSocket:    {0}" -f (Get-Field (Get-Field $hostInfo 'remoteSocket') 'path'))
}
else {
    Write-Host 'podman info failed: the machine is probably not running.'
}

Write-Host ''
Write-Host '== Docker-compatible connections =='
& $podman system connection list 2>&1
Write-Host ''

if ($isWindowsHost) {
    Write-Host '== Named pipes =='
    $pipes = @()
    try {
        $pipes = [System.IO.Directory]::GetFiles('\\.\pipe\') |
            Where-Object { $_ -match 'podman|docker' } |
            ForEach-Object { $_.Replace('\\.\pipe\', '') }
    }
    catch {
        Write-Host 'Could not enumerate named pipes.'
    }

    if ($pipes.Count -eq 0) {
        Write-Host 'No podman/docker named pipe found. Start the Podman machine: podman machine start'
    }
    else {
        foreach ($pipeName in $pipes) {
            $reachable = Test-Pipe -Name $pipeName
            $status = if ($reachable) { 'REACHABLE' } else { 'DENIED/CLOSED' }
            Write-Host ("{0,-32} {1}" -f $pipeName, $status)
        }
    }
}
else {
    Write-Host '== Unix sockets =='
    foreach ($candidate in @('/run/user/1000/podman/podman.sock', '/var/run/podman/podman.sock', '/var/run/docker.sock')) {
        $status = if (Test-Path -LiteralPath $candidate) { 'exists' } else { 'missing' }
        Write-Host ("{0,-40} {1}" -f $candidate, $status)
    }
}

Write-Host ''
Write-Host '== Environment =='
foreach ($name in @('DOCKER_HOST', 'MECHANIC_CONTAINER_ENGINE', 'PODMAN_PATH', 'TESTCONTAINERS_RYUK_DISABLED', 'TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE')) {
    $value = [Environment]::GetEnvironmentVariable($name)
    Write-Host ("{0,-38} {1}" -f $name, $(if ($value) { $value } else { '<unset>' }))
}

Write-Host ''
Write-Host '== Verdict =='
if ($isWindowsHost) {
    $podmanPipe = $null
    if ($machine) {
        $pipePath = Get-Field (Get-Field (Get-Field $machine 'ConnectionInfo') 'PodmanPipe') 'Path'
        if ($pipePath) { $podmanPipe = ($pipePath -split '\\')[-1] }
    }

    if (-not $podmanPipe) { $podmanPipe = 'podman-machine-default' }

    if (Test-Pipe -Name $podmanPipe) {
        Write-Host 'Podman pipe is reachable from this process: run the tests here.' -ForegroundColor Green
    }
    elseif (Test-Pipe -Name 'docker_engine') {
        Write-Host 'Podman pipe is denied, but the docker_engine pipe is reachable.' -ForegroundColor Yellow
        Write-Host 'The test bootstrap falls back to docker_engine automatically; enable Docker'
        Write-Host 'Compatibility in Podman Desktop if that pipe belongs to Podman.'
    }
    else {
        Write-Host 'Neither the Podman pipe nor docker_engine is reachable from this process.' -ForegroundColor Red
        Write-Host 'Fixes, in order:'
        Write-Host '  1. Start the machine: podman machine start'
        Write-Host '  2. Run the test runner and Podman Desktop in the same session and the same'
        Write-Host '     elevation level (all normal, or all elevated).'
        Write-Host '  3. Enable Docker Compatibility in Podman Desktop (Settings) so a docker_engine'
        Write-Host '     pipe is available; the bootstrap probes and uses it as fallback.'
        Write-Host '  4. Restart the machine: podman machine stop; podman machine start'
        Write-Host 'Without an engine the database tests are reported as Skipped, not Failed.'
    }
}
else {
    Write-Host 'Check that /run/user/<uid>/podman/podman.sock exists and that the test process'
    Write-Host 'can access it (same user and session). Without an engine the database tests'
    Write-Host 'are reported as Skipped, not Failed.'
}
