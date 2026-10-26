#Requires -Version 5.1
<#
.SYNOPSIS
    Prepares Podman for the .NET Testcontainers suites (test host friendly).

.DESCRIPTION
    1. Starts the Podman machine when needed.
    2. Chooses how the tests reach the machine:
       - tcp (default): starts `podman system service` inside the machine and
         points DOCKER_HOST at tcp://localhost:2375. The vstest test host runs at
         low integrity on Windows, which cannot write the named pipe of a Podman
         machine started from an elevated process. TCP has no such restriction,
         so this transport works from Visual Studio, `dotnet test` and CI alike.
       - pipe: uses the machine's named pipe (only works when the test process
         can open it read-write).
       - auto: pipe when it is writable from this process, otherwise tcp.
    3. Creates %TEMP%\Low with a low mandatory label, because a low integrity
       test host cannot write %TEMP% itself but can write inside that folder.
    4. Removes or repairs stale test temp folders.
    5. Writes Podman.runsettings pinning DOCKER_HOST (and Ryuk off) for the
       chosen transport, so Visual Studio does not depend on its own environment.

.PARAMETER Transport
    tcp (default), pipe, or auto.

.PARAMETER Port
    TCP port of the Podman API service. Default 2375.

.PARAMETER MachineName
    Podman machine name. Defaults to the machine marked default.

.PARAMETER TimeoutSeconds
    How long to wait for the Docker-compatible API to answer.

.PARAMETER KeepRyuk
    Keeps the Testcontainers Ryuk reaper enabled (not recommended for Podman).

.PARAMETER NoUserScope
    Skips writing DOCKER_HOST to the user environment (runsettings still pins it).

.PARAMETER SmokeTest
    Runs a container after setup to prove the engine works.

.PARAMETER DotnetTestTarget
    Path passed to `dotnet test` (a .sln or .csproj). Omit to only set up.

.PARAMETER ResetTempFolders
    Test temp folder names that must be writable before a run.
#>
param(
    [ValidateSet('tcp', 'pipe', 'auto')]
    [string]$Transport = 'tcp',
    [int]$Port = 2375,
    [string]$MachineName,
    [int]$TimeoutSeconds = 180,
    [switch]$KeepRyuk,
    [switch]$NoUserScope,
    [switch]$SmokeTest,
    [string]$DotnetTestTarget,
    [string[]]$ResetTempFolders = @('dbfactory-tests', 'audit-archive-tests', 'dotenv-loader-tests', 'ledka-architecture-tests')
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Good {
    param([string]$Message)
    Write-Host "    [OK]   $Message" -ForegroundColor Green
}

function Write-Note {
    param([string]$Message)
    Write-Host "    [INFO] $Message" -ForegroundColor Gray
}

function Write-Caution {
    param([string]$Message)
    Write-Host "    [WARN] $Message" -ForegroundColor Yellow
}

function Invoke-Podman {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $raw = @(& podman @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
    $stdout = @($raw | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { [string]$_ })
    $stderr = @($raw | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { [string]$_ })
    [pscustomobject]@{
        ExitCode = $exitCode
        StdOut   = ($stdout -join [Environment]::NewLine).Trim()
        StdErr   = ($stderr -join [Environment]::NewLine).Trim()
    }
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites"
    $podmanCommand = Get-Command podman -ErrorAction SilentlyContinue
    if (-not $podmanCommand) {
        throw "podman was not found in PATH. Install Podman Desktop or Podman for Windows, then open a new terminal."
    }
    $version = Invoke-Podman -Arguments @('--version')
    Write-Good $version.StdOut
}

function Get-PodmanMachines {
    $result = Invoke-Podman -Arguments @('machine', 'list', '--format', 'json')
    if ($result.ExitCode -ne 0) {
        throw "Failed to list Podman machines: $($result.StdErr)"
    }
    if ([string]::IsNullOrWhiteSpace($result.StdOut)) {
        return @()
    }
    return @(ConvertFrom-Json -InputObject $result.StdOut)
}

function Initialize-PodmanMachine {
    Write-Step "Detecting Podman machine"
    $machines = @(Get-PodmanMachines)
    if ($machines.Count -eq 0) {
        Write-Note "No Podman machine exists. Initializing the default machine (this can take several minutes)..."
        $init = Invoke-Podman -Arguments @('machine', 'init')
        if ($init.ExitCode -ne 0) {
            throw "podman machine init failed: $($init.StdErr)"
        }
        $machines = @(Get-PodmanMachines)
    }

    if ($MachineName) {
        $selected = $machines | Where-Object { $_.Name -eq $MachineName } | Select-Object -First 1
        if (-not $selected) {
            throw "Podman machine '$MachineName' was not found."
        }
    }
    else {
        $selected = $machines | Where-Object { $_.Default -eq $true } | Select-Object -First 1
        if (-not $selected) {
            $selected = $machines | Select-Object -First 1
        }
    }
    if (-not $selected) {
        throw "No usable Podman machine could be determined."
    }

    Write-Good "Using machine '$($selected.Name)'"
    if ($selected.Running) {
        Write-Good "Machine is already running"
    }
    else {
        Write-Note "Machine is stopped. Starting..."
        $start = Invoke-Podman -Arguments @('machine', 'start', $selected.Name)
        if ($start.ExitCode -ne 0) {
            throw "podman machine start failed: $($start.StdErr) $($start.StdOut)"
        }
        Write-Good "Machine started"
    }
    return $selected.Name
}

function Get-PodmanPipeCandidates {
    param([string]$Name)
    $candidates = New-Object System.Collections.Generic.List[string]
    $inspect = Invoke-Podman -Arguments @('machine', 'inspect', $Name)
    if ($inspect.ExitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($inspect.StdOut)) {
        $details = @(ConvertFrom-Json -InputObject $inspect.StdOut)
        if ($details.Count -gt 0 -and $details[0].ConnectionInfo -and $details[0].ConnectionInfo.PodmanPipe) {
            $path = [string]$details[0].ConnectionInfo.PodmanPipe.Path
            if ($path -match 'pipe[\\/](.+)$') {
                $candidates.Add($Matches[1])
            }
        }
    }
    foreach ($pipe in @("podman-machine-default", "podman-$Name")) {
        if (-not $candidates.Contains($pipe)) {
            $candidates.Add($pipe)
        }
    }
    return , $candidates.ToArray()
}

function Test-DockerApiPipe {
    param([Parameter(Mandatory)][string]$PipeName)
    $stream = $null
    try {
        $stream = New-Object System.IO.Pipes.NamedPipeClientStream('.', $PipeName, [System.IO.Pipes.PipeDirection]::InOut, [System.IO.Pipes.PipeOptions]::None)
        $stream.Connect(2000)
        $request = [System.Text.Encoding]::ASCII.GetBytes("GET /_ping HTTP/1.1`r`nHost: docker`r`nConnection: close`r`n`r`n")
        $stream.Write($request, 0, $request.Length)
        $stream.Flush()
        $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::ASCII)
        $readTask = $reader.ReadLineAsync()
        if (-not $readTask.Wait(3000)) {
            return $false
        }
        return ($readTask.Result -match '^HTTP/1\.[01] 200')
    }
    catch {
        return $false
    }
    finally {
        if ($stream) {
            $stream.Dispose()
        }
    }
}

function Test-DockerApiTcp {
    param([string]$Uri)
    try {
        $request = Invoke-WebRequest -Uri $Uri -TimeoutSec 5 -UseBasicParsing
        return ($request.StatusCode -eq 200)
    }
    catch {
        return $false
    }
}

function Wait-ForDockerApi {
    param([string[]]$PipeNames, [int]$Timeout)
    Write-Step "Waiting for the Docker-compatible API on the Podman pipe"
    $deadline = (Get-Date).AddSeconds($Timeout)
    do {
        foreach ($pipe in $PipeNames) {
            if (Test-DockerApiPipe -PipeName $pipe) {
                return $pipe
            }
        }
        Start-Sleep -Seconds 3
    } while ((Get-Date) -lt $deadline)
    return $null
}

function Wait-ForDockerApiTcp {
    param([string]$PingUri, [int]$Timeout)
    Write-Step "Waiting for the Podman API service on $PingUri"
    $deadline = (Get-Date).AddSeconds($Timeout)
    do {
        if (Test-DockerApiTcp -Uri $PingUri) {
            return $true
        }
        Start-Sleep -Seconds 3
    } while ((Get-Date) -lt $deadline)
    return $false
}

function Start-PodmanApiService {
    param([string]$Port)
    Write-Step "Starting the Podman API service over TCP (survives the test host)"
    $uri = "http://localhost:$Port/_ping"
    if (Test-DockerApiTcp -Uri $uri) {
        Write-Good "A Podman API service is already listening on port $Port"
        return
    }

    Write-Note "Starting 'podman system service' inside the machine (detached, nohup)..."
    $clientTimeout = [Math]::Max(30, $TimeoutSeconds)
    $remote = "nohup podman system service -t 0 tcp:0.0.0.0:$Port >/tmp/podman-system-service-$Port.log 2>&1 &"
    & podman machine ssh -- bash -lc $remote 2>&1 | ForEach-Object { Write-Note "podman machine ssh: $_" }

    if (-not (Wait-ForDockerApiTcp -PingUri $uri -Timeout $clientTimeout)) {
        throw "The Podman API service did not answer on $uri. Check inside the machine: podman machine ssh -- podman system service -t 0 tcp:0.0.0.0:$Port"
    }
    Write-Good "Podman API service answers on $uri"
}

function Set-DockerHostEnvironment {
    param([string]$DockerHost)
    Write-Step "Configuring DOCKER_HOST and Testcontainers environment"
    $env:DOCKER_HOST = $DockerHost
    if (-not $NoUserScope) {
        [Environment]::SetEnvironmentVariable('DOCKER_HOST', $DockerHost, 'User')
        Write-Good "DOCKER_HOST = $DockerHost (process and user scope)"
    }
    else {
        Write-Good "DOCKER_HOST = $DockerHost (process scope; runsettings pins it for Visual Studio)"
    }

    if ($KeepRyuk) {
        Remove-Item Env:TESTCONTAINERS_RYUK_DISABLED -ErrorAction SilentlyContinue
        if (-not $NoUserScope) {
            [Environment]::SetEnvironmentVariable('TESTCONTAINERS_RYUK_DISABLED', $null, 'User')
        }
        Write-Note "Ryuk is enabled (as requested with -KeepRyuk)"
    }
    else {
        $env:TESTCONTAINERS_RYUK_DISABLED = 'true'
        if (-not $NoUserScope) {
            [Environment]::SetEnvironmentVariable('TESTCONTAINERS_RYUK_DISABLED', 'true', 'User')
        }
        Write-Good "Ryuk disabled (recommended for Podman)"
    }
    return $DockerHost
}

function Update-TestcontainersProperties {
    param([string]$DockerHost)
    Write-Step "Writing Testcontainers properties file"
    $path = Join-Path $env:USERPROFILE '.testcontainers.properties'
    $managedKeys = @('docker.host', 'ryuk.disabled')
    $lines = New-Object System.Collections.Generic.List[string]

    if (Test-Path -LiteralPath $path) {
        Copy-Item -LiteralPath $path -Destination "$path.bak" -Force
        Write-Note "Existing file backed up to $path.bak"
        foreach ($line in @(Get-Content -LiteralPath $path)) {
            $trimmed = $line.Trim()
            $isManaged = $false
            foreach ($key in $managedKeys) {
                if ($trimmed -match ('^' + [regex]::Escape($key) + '\s*[=:]')) {
                    $isManaged = $true
                    break
                }
            }
            if (-not $isManaged) {
                $lines.Add($line)
            }
        }
    }

    $lines.Add("docker.host=$DockerHost")
    if (-not $KeepRyuk) {
        $lines.Add('ryuk.disabled=true')
    }
    Set-Content -LiteralPath $path -Value $lines.ToArray() -Encoding ASCII
    Write-Good "Updated $path"
}

function Test-FolderWritable {
    param([string]$Path)
    $probe = Join-Path $Path ("probe-" + [guid]::NewGuid().ToString('N'))
    try {
        New-Item -ItemType Directory -Path $probe -ErrorAction Stop | Out-Null
        Remove-Item -LiteralPath $probe -Recurse -Force -ErrorAction SilentlyContinue
        return $true
    }
    catch {
        return $false
    }
}

function Reset-TestTempFolders {
    param([string[]]$Names)
    Write-Step "Verifying test temp folders"
    $roots = @((Join-Path $env:TEMP 'Low'), $env:TEMP)
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }
        foreach ($name in $Names) {
            $path = Join-Path $root $name
            if (-not (Test-Path -LiteralPath $path)) {
                continue
            }
            if (Test-FolderWritable -Path $path) {
                Write-Good "$path is writable"
                continue
            }
            Write-Caution "$path is not writable by the current user"
            try {
                Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
                Write-Good "Removed $path"
            }
            catch {
                Write-Note "Removal failed. Resetting ACL and granting Full Control to $($env:USERNAME)..."
                & icacls $path /reset /T /C | Out-Null
                & icacls $path /grant "$($env:USERNAME):(OI)(CI)F" /T /C | Out-Null
                if (Test-FolderWritable -Path $path) {
                    Write-Good "Access granted on $path"
                }
                else {
                    Write-Caution "Still not writable. Run PowerShell as administrator once and execute: Remove-Item -LiteralPath '$path' -Recurse -Force"
                }
            }
        }
    }
}

function Initialize-LowTempFolder {
    Write-Step "Preparing %TEMP%\Low for the low-integrity test host"
    $path = Join-Path $env:TEMP 'Low'
    $probe = Join-Path $path ("writable-probe-" + [guid]::NewGuid().ToString('N'))
    $created = $false

    if (-not (Test-Path -LiteralPath $path)) {
        try {
            New-Item -ItemType Directory -Path $path -ErrorAction Stop | Out-Null
            $created = $true
        }
        catch {
            Write-Caution "Could not create $path. Fix its permissions manually, or run this script once as administrator: New-Item -ItemType Directory -Path '$path'"
        }
    }

    if (Test-Path -LiteralPath $path) {
        if ($created) {
            & icacls $path /grant "$($env:USERNAME):(OI)(CI)F" /C 2>&1 | Out-Null
            # Low mandatory label: the low-integrity vstest test host can write here,
            # and every higher integrity process can write here as well.
            & icacls $path /setintegritylevel low /C 2>&1 | Out-Null
        }
        try {
            New-Item -ItemType Directory -Path $probe -ErrorAction Stop | Out-Null
            Remove-Item -LiteralPath $probe -Recurse -Force -ErrorAction SilentlyContinue
            Write-Good "$path exists and is writable"
        }
        catch {
            Write-Caution "$path is not writable from this process: $($_.Exception.Message)"
        }
    }
}

function Write-RunSettings {
    param([string]$Directory, [string]$DockerHost)
    Write-Step "Writing Podman.runsettings for test runs"
    $path = Join-Path $Directory 'Podman.runsettings'
    $envLines = New-Object System.Collections.Generic.List[string]
    $envLines.Add("        <DOCKER_HOST>$DockerHost</DOCKER_HOST>")
    if (-not $KeepRyuk) {
        $envLines.Add('        <TESTCONTAINERS_RYUK_DISABLED>true</TESTCONTAINERS_RYUK_DISABLED>')
    }
    $content = @(
        '<?xml version="1.0" encoding="utf-8"?>',
        '<RunSettings>',
        '  <RunConfiguration>',
        '    <EnvironmentVariables>'
    ) + $envLines.ToArray() + @(
        '    </EnvironmentVariables>',
        '  </RunConfiguration>',
        '</RunSettings>'
    )
    Set-Content -LiteralPath $path -Value $content -Encoding ASCII
    Write-Good "Created $path"
    return $path
}

function Invoke-SmokeTest {
    Write-Step "Running optional container smoke test (local images only, no pull)"
    $run = Invoke-Podman -Arguments @('run', '--rm', '--pull=never', 'docker.io/library/alpine:latest', 'echo', 'podman-ok')
    if ($run.ExitCode -ne 0) {
        Write-Caution "Smoke test skipped: $($run.StdErr)"
        return
    }
    if ($run.StdOut -match 'podman-ok') {
        Write-Good "Container ran successfully"
    }
    else {
        Write-Caution "Unexpected output: $($run.StdOut)"
    }
}

function Invoke-DotnetTests {
    param([string]$Target, [string]$RunSettingsPath)
    Write-Step "Running dotnet test on $Target"
    & dotnet test $Target --settings $RunSettingsPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

function Test-VisualStudioRunning {
    $vs = Get-Process -Name devenv -ErrorAction SilentlyContinue
    if ($vs) {
        Write-Caution "Visual Studio is running. Select Podman.runsettings in VS (see below); a restart is also recommended."
    }
}

try {
    Test-Prerequisites
    $selectedMachine = Initialize-PodmanMachine
    Initialize-LowTempFolder

    $dockerHost = $null
    $source = $null

    if ($Transport -eq 'pipe') {
        $candidates = Get-PodmanPipeCandidates -Name $selectedMachine
        $pipe = Wait-ForDockerApi -PipeNames $candidates -Timeout $TimeoutSeconds
        if (-not $pipe) {
            throw "No Docker-compatible API responded on pipes: $($candidates -join ', '). The vstest test host cannot write these pipes from a low integrity process: rerun with -Transport tcp."
        }
        Write-Good "Docker-compatible API is reachable on pipe '$pipe'"
        $dockerHost = "npipe://./pipe/$pipe"
        $source = "Podman machine '$selectedMachine' named pipe"
    }
    elseif ($Transport -eq 'auto') {
        $candidates = Get-PodmanPipeCandidates -Name $selectedMachine
        $pipe = $null
        foreach ($candidate in $candidates) {
            if (Test-DockerApiPipe -PipeName $candidate) {
                $pipe = $candidate
                break
            }
        }
        if ($pipe) {
            Write-Good "Docker-compatible API is reachable on pipe '$pipe'"
            $dockerHost = "npipe://./pipe/$pipe"
            $source = "Podman machine '$selectedMachine' named pipe"
        }
        else {
            Write-Note "Named pipes are not writable from this process; falling back to the TCP API service."
            $dockerHost = "tcp://localhost:$Port"
            $source = "Podman machine API over TCP (podman system service)"
        }
    }
    else {
        $dockerHost = "tcp://localhost:$Port"
        $source = "Podman machine API over TCP (podman system service)"
    }

    if ($dockerHost -like 'tcp://*') {
        Start-PodmanApiService -Port $Port
    }

    $resolvedHost = Set-DockerHostEnvironment -DockerHost $dockerHost
    Write-Good "Engine: $source via $resolvedHost"
    Update-TestcontainersProperties -DockerHost $resolvedHost
    Reset-TestTempFolders -Names $ResetTempFolders

    if ($DotnetTestTarget) {
        $targetPath = (Resolve-Path -LiteralPath $DotnetTestTarget).Path
        $runSettingsDir = Split-Path -Parent $targetPath
    }
    else {
        $targetPath = $null
        $runSettingsDir = (Get-Location).Path
    }
    $runSettingsPath = Write-RunSettings -Directory $runSettingsDir -DockerHost $resolvedHost

    if ($SmokeTest) {
        Invoke-SmokeTest
    }
    if ($targetPath) {
        Invoke-DotnetTests -Target $targetPath -RunSettingsPath $runSettingsPath
    }

    Test-VisualStudioRunning
    Write-Host ""
    Write-Host "Setup completed. Podman is ready for Testcontainers." -ForegroundColor Green
    Write-Host "In Visual Studio: Test > Configure Run Settings > Select Solution Wide runsettings File > $runSettingsPath" -ForegroundColor Yellow
    exit 0
}
catch {
    Write-Host ""
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
