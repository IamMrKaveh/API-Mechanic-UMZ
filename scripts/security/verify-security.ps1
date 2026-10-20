<#
.SYNOPSIS
    Runs the security-audit "phase 5" verification gate for this repository.

.DESCRIPTION
    One command that reproduces the audit's final acceptance checklist:

      1. repository hygiene         - .env / appsettings.Production.json / otp.json are not tracked
      2. configuration placeholders - no live secret in the committed appsettings.json
      3. build                      - dotnet build -c Release -warnaserror
      4. tests                      - full test suite (database tests skip without Docker)
      5. dependency audit           - dotnet list package --vulnerable --include-transitive
      6. gitleaks working tree      - no secret in the sources that would be committed
      7. gitleaks git history       - SEC-01 debt; reports PENDING until the history rewrite

    Exit code 0 means "no blocking problem". Historical leaks are reported as
    PENDING (the pre-rotation debt described in SECURITY-AUDIT-REPORT.md) unless
    -FailOnHistoricalSecrets is supplied, which turns them into a hard failure for
    the release checklist (see docs/07-operations/security-verification.md).

.PARAMETER SkipTests
    Skip step 4 (useful for a fast secret-only check).

.PARAMETER FailOnHistoricalSecrets
    Treat findings in the Git history as a failure. Use this in a release gate;
    it stays red until the SEC-01 history rewrite has been performed.

.PARAMETER GitleaksPath
    Path to a gitleaks executable. Defaults to `gitleaks` on PATH, then to the
    pinned download, then to an automatic download of the pinned release.

.PARAMETER NoDownload
    Never download gitleaks; secret steps are then reported as SKIPPED.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1 -SkipTests -FailOnHistoricalSecrets
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$FailOnHistoricalSecrets,
    [string]$GitleaksPath,
    [switch]$NoDownload
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

# Pinned scanner version + SHA256 of the official release assets (see
# https://github.com/gitleaks/gitleaks/releases). Bump both together.
$gitleaksVersion = '8.30.1'
$gitleaksAssetSha256 = @{
    'windows_x64' = 'd29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e'
    'linux_x64'   = '551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb'
}

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [string]$Check,
        [string]$Status,
        [string]$Detail
    )

    $results.Add([pscustomobject]@{ Check = $Check; Status = $Status; Detail = $Detail })
    Write-Host ("[{0}] {1} - {2}" -f $Status, $Check, $Detail)
}

<#
    Native commands write to stderr; under ErrorActionPreference=Stop that can be
    turned into a terminating error (Windows PowerShell especially). This helper
    captures both streams as plain strings and returns the real exit code.
#>
function Invoke-Native {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = & $FilePath @Arguments 2>&1 | ForEach-Object { "$_" }
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }

    return [pscustomobject]@{
        Output   = ($lines -join [Environment]::NewLine)
        ExitCode = $exitCode
    }
}

function Invoke-Checked {
    param(
        [string]$Check,
        [scriptblock]$Action
    )

    try {
        $detail = & $Action
        Add-Result -Check $Check -Status 'PASS' -Detail ([string]$detail)
        return $true
    }
    catch {
        Add-Result -Check $Check -Status 'FAIL' -Detail $_.Exception.Message
        return $false
    }
}

function ConvertFrom-Jsonc {
    <#
        appsettings.json is JSONC (line comments). System.Text.Json is not
        available on Windows PowerShell 5.1, so strip comments outside strings.
    #>
    param([string]$Content)

    $builder = New-Object System.Text.StringBuilder
    $inString = $false
    $escaped = $false
    $index = 0

    while ($index -lt $Content.Length) {
        $character = $Content[$index]

        if ($inString) {
            [void]$builder.Append($character)
            if ($escaped) { $escaped = $false }
            elseif ($character -eq '\') { $escaped = $true }
            elseif ($character -eq '"') { $inString = $false }
            $index++
            continue
        }

        if ($character -eq '"') {
            $inString = $true
            [void]$builder.Append($character)
            $index++
            continue
        }

        if ($character -eq '/' -and ($index + 1) -lt $Content.Length) {
            $next = $Content[$index + 1]
            if ($next -eq '/') {
                while ($index -lt $Content.Length -and $Content[$index] -ne "`n") { $index++ }
                continue
            }
            if ($next -eq '*') {
                $index += 2
                while (($index + 1) -lt $Content.Length -and -not ($Content[$index] -eq '*' -and $Content[$index + 1] -eq '/')) { $index++ }
                $index += 2
                continue
            }
        }

        [void]$builder.Append($character)
        $index++
    }

    return ($builder.ToString() | ConvertFrom-Json)
}

function Get-Leaf {
    param(
        $Node,
        [string[]]$Path
    )

    foreach ($name in $Path) {
        if ($null -eq $Node) { return $null }
        $property = $Node.PSObject.Properties[$name]
        if ($null -eq $property) { return $null }
        $Node = $property.Value
    }

    return $Node
}

function Resolve-Gitleaks {
    if ($GitleaksPath) {
        if (-not (Test-Path -LiteralPath $GitleaksPath)) {
            throw "GitleaksPath does not exist: $GitleaksPath"
        }
        return (Resolve-Path -LiteralPath $GitleaksPath).Path
    }

    $command = Get-Command gitleaks -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $isWindowsHost = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
    $assetName = if ($isWindowsHost) { 'windows_x64' } else { 'linux_x64' }
    $executableName = if ($isWindowsHost) { 'gitleaks.exe' } else { 'gitleaks' }
    $localAppData = [System.Environment]::GetFolderPath('LocalApplicationData')
    $toolsRoot = Join-Path $localAppData ("gitleaks\v" + $gitleaksVersion)
    $executablePath = Join-Path $toolsRoot $executableName

    if (Test-Path -LiteralPath $executablePath) { return $executablePath }
    if ($NoDownload) { return $null }

    $extension = if ($isWindowsHost) { 'zip' } else { 'tar.gz' }
    $fileName = "gitleaks_${gitleaksVersion}_${assetName}.$extension"
    $url = "https://github.com/gitleaks/gitleaks/releases/download/v$gitleaksVersion/$fileName"
    $expected = $gitleaksAssetSha256[$assetName]
    if (-not $expected) { throw "No pinned SHA256 for asset '$assetName'." }

    Write-Host "Downloading gitleaks v$gitleaksVersion ($assetName)..."
    New-Item -ItemType Directory -Path $toolsRoot -Force | Out-Null
    $archivePath = Join-Path $toolsRoot $fileName
    Invoke-WebRequest -Uri $url -OutFile $archivePath -UseBasicParsing

    $actual = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $expected.ToLowerInvariant()) {
        Remove-Item -LiteralPath $archivePath -Force -ErrorAction SilentlyContinue
        throw "Checksum mismatch for $fileName (expected $expected, got $actual)."
    }

    if ($isWindowsHost) {
        Expand-Archive -LiteralPath $archivePath -DestinationPath $toolsRoot -Force
    }
    else {
        $extract = Invoke-Native -FilePath 'tar' -Arguments @('-xzf', $archivePath, '-C', $toolsRoot)
        if ($extract.ExitCode -ne 0) { throw 'tar failed to extract the gitleaks archive.' }
    }

    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "gitleaks executable not found after extraction: $executablePath"
    }

    return $executablePath
}

function Invoke-Gitleaks {
    param(
        [string]$Executable,
        [string[]]$Arguments,
        [string]$ReportPath
    )

    $allArguments = @($Arguments) + @(
        '--config', '.gitleaks.toml',
        '--redact',
        '--no-banner',
        '--report-format', 'json',
        '--report-path', $ReportPath
    )

    $result = Invoke-Native -FilePath $Executable -Arguments $allArguments

    $findingCount = 0
    if (Test-Path -LiteralPath $ReportPath) {
        $raw = Get-Content -LiteralPath $ReportPath -Raw
        if ($raw -and $raw.Trim().Length -gt 2) {
            $parsed = $raw | ConvertFrom-Json
            $findingCount = @($parsed).Count
        }
        Remove-Item -LiteralPath $ReportPath -Force -ErrorAction SilentlyContinue
    }

    if ($result.ExitCode -ne 0 -and $findingCount -eq 0) {
        throw ("gitleaks failed with exit code {0}: {1}" -f $result.ExitCode, $result.Output.Trim())
    }

    return $findingCount
}

<#
    Copies every file git considers part of the repository (tracked files plus
    untracked files that are not gitignored) into a temporary tree, so the
    working-tree secret scan only looks at content that could be committed.
    Local .env / appsettings.Production.json files never land in that tree.
#>
function New-CommitCandidateExport {
    $root = Join-Path ([System.IO.Path]::GetTempPath()) ('mechanic-tree-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $root -Force | Out-Null

    $tracked = @(& git ls-files)
    $untracked = @(& git ls-files --others --exclude-standard)
    $files = @($tracked + $untracked | Sort-Object -Unique)

    foreach ($relative in $files) {
        if (-not (Test-Path -LiteralPath $relative -PathType Leaf)) { continue }

        $destination = Join-Path $root $relative
        $directory = Split-Path -Parent $destination
        if (-not (Test-Path -LiteralPath $directory)) {
            New-Item -ItemType Directory -Path $directory -Force | Out-Null
        }
        Copy-Item -LiteralPath $relative -Destination $destination -Force
    }

    return $root
}

Write-Host ''
Write-Host '== Mechanic.Api security verification (audit phase 5) ==' -ForegroundColor Cyan
Write-Host "Repository: $repoRoot"
Write-Host ''

# 1. Repository hygiene -------------------------------------------------------
Invoke-Checked -Check 'Repo hygiene (.env untracked)' -Action {
    $tracked = @(& git ls-files)
    if ($tracked -contains '.env' -or $tracked -contains 'MainApi/.env') {
        throw '.env is tracked; secrets must live outside the repository.'
    }

    if (-not (Test-Path -LiteralPath '.git')) {
        throw 'Not a git working tree.'
    }

    'no .env in the index'
} | Out-Null

Invoke-Checked -Check 'Repo hygiene (no production settings or otp.json)' -Action {
    $forbidden = @('otp.json', 'appsettings.Production.json', 'appsettings.Staging.json')
    $tracked = @(& git ls-files)
    $hits = @()
    foreach ($path in $tracked) {
        foreach ($name in $forbidden) {
            if ($path -eq $name -or $path.EndsWith('/' + $name)) { $hits += $path }
        }
    }

    if ($hits.Count -gt 0) {
        throw ('These files must not be tracked: ' + ($hits -join ', '))
    }

    'none tracked'
} | Out-Null

Invoke-Checked -Check 'Repo hygiene (no ignored build sources)' -Action {
    # A .gitignore rule such as "Database/" also matches Tests/.../Database and
    # silently keeps build-critical sources out of the repository.
    $ignored = @(& git ls-files --others --ignored --exclude-standard)
    $suspicious = @($ignored | Where-Object {
        $_ -match '\.(cs|csproj|props|targets|sln|sql|sh|ps1)$' -and
        $_ -notmatch '^(\.vs|\.dsh-temp|\.workbuddy-ai)/' -and
        $_ -notmatch '(^|/)(bin|obj|node_modules|TestResults|Logs?|keys)/'
    })

    if ($suspicious.Count -gt 0) {
        throw ('These ignored files look like build inputs: ' + (($suspicious | Select-Object -First 10) -join ', '))
    }

    'no ignored build sources'
} | Out-Null

# 2. Committed configuration placeholders -------------------------------------
Invoke-Checked -Check 'appsettings.json holds no secrets' -Action {
    $settingsPath = 'Presentation/appsettings.json'
    if (-not (Test-Path -LiteralPath $settingsPath)) { throw "$settingsPath is missing." }

    $settings = ConvertFrom-Jsonc -Content (Get-Content -LiteralPath $settingsPath -Raw)

    $secretPaths = @(
        @('Jwt', 'Key'),
        @('Auth', 'OtpPepper'),
        @('Storage', 'AccessKey'),
        @('Storage', 'SecretKey'),
        @('Kavenegar', 'ApiKey'),
        @('AWS', 'S3', 'AccessKey'),
        @('AWS', 'S3', 'SecretKey')
    )

    $violations = @()
    foreach ($secretPath in $secretPaths) {
        $value = Get-Leaf -Node $settings -Path $secretPath
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            $violations += ($secretPath -join ':')
        }
    }

    if ($violations.Count -gt 0) {
        throw ('Populated secret placeholders: ' + ($violations -join ', '))
    }

    'all secret placeholders empty'
} | Out-Null

# 3. Build --------------------------------------------------------------------
$buildSucceeded = Invoke-Checked -Check 'dotnet build -warnaserror' -Action {
    $build = Invoke-Native -FilePath 'dotnet' -Arguments @('build', 'API.sln', '-c', 'Release', '-warnaserror', '--nologo')
    if ($build.ExitCode -ne 0) {
        $tail = ($build.Output -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -Last 15) -join [Environment]::NewLine
        throw ('build failed:' + [Environment]::NewLine + $tail)
    }
    'build succeeded with zero warnings'
}

# 4. Tests --------------------------------------------------------------------
if ($SkipTests) {
    Add-Result -Check 'dotnet test' -Status 'SKIPPED' -Detail 'disabled with -SkipTests'
    Add-Result -Check 'security invariants (phase 6)' -Status 'SKIPPED' -Detail 'disabled with -SkipTests'
}
elseif (-not $buildSucceeded) {
    Add-Result -Check 'dotnet test' -Status 'SKIPPED' -Detail 'build failed'
    Add-Result -Check 'security invariants (phase 6)' -Status 'SKIPPED' -Detail 'build failed'
}
else {
    # Phase 6 invariants run first: they are fast and point at the exact file/line
    # when a documented security strength has regressed.
    Invoke-Checked -Check 'security invariants (phase 6)' -Action {
        $invariants = Invoke-Native -FilePath 'dotnet' -Arguments @(
            'test', 'Tests/Tests.csproj', '-c', 'Release', '--no-build',
            '--filter', 'FullyQualifiedName~Tests.Security.',
            '--logger', 'console;verbosity=minimal'
        )

        $summary = ($invariants.Output -split "`r?`n" | Where-Object { $_ -match 'Passed!\s|Failed!\s' } | Select-Object -Last 1)
        if ($invariants.ExitCode -ne 0) {
            $tail = ($invariants.Output -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -Last 10) -join [Environment]::NewLine
            throw ('security invariants failed:' + [Environment]::NewLine + $tail)
        }

        $summary.Trim()
    } | Out-Null
    Invoke-Checked -Check 'dotnet test' -Action {
        $resultsDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('mechanic-tests-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null

        $test = Invoke-Native -FilePath 'dotnet' -Arguments @(
            'test', 'Tests/Tests.csproj', '-c', 'Release', '--no-build',
            '--logger', 'console;verbosity=minimal',
            '--logger', 'trx;LogFileName=results.trx',
            '--results-directory', $resultsDirectory
        )

        $summary = ($test.Output -split "`r?`n" | Where-Object { $_ -match 'Failed!\s|Passed!\s' } | Select-Object -Last 1)
        if (-not $summary) {
            $summary = ($test.Output -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -Last 1)
        }

        # The database-backed tests need a container engine (Podman by default, Docker
        # as fallback). Without a reachable engine they report an engine-unavailable
        # failure; count those as environment skips, but keep every other failure hard.
        $trxPath = Join-Path $resultsDirectory 'results.trx'
        $dockerSkipped = 0
        $realFailures = @()

        if (Test-Path -LiteralPath $trxPath) {
            [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
            $xmlNamespace = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
            $xmlNamespace.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')

            foreach ($result in @($trx.SelectNodes('//t:UnitTestResult[@outcome="Failed"]', $xmlNamespace))) {
                $message = ''
                if ($result.Output -and $result.Output.ErrorInfo) {
                    $message = [string]$result.Output.ErrorInfo.Message
                }

                if ($message -match 'No container engine reachable' -or
                    $message -match 'No Docker-API-compatible engine reachable' -or
                    $message -match 'DockerUnavailableException') {
                    $dockerSkipped++
                }
                else {
                    $realFailures += [string]$result.testName
                }
            }
        }
        else {
            $realFailures += 'test run produced no TRX result file'
        }

        if ($realFailures.Count -gt 0) {
            throw ('tests failed: ' + (($realFailures | Select-Object -First 5) -join ', '))
        }

        if ($test.ExitCode -ne 0 -and $dockerSkipped -eq 0) {
            throw ('tests failed: ' + $summary.Trim())
        }

        if ($dockerSkipped -gt 0) {
            return ("{0} (skipped {1} container-engine-dependent database test(s): no reachable Podman/Docker engine)" -f $summary.Trim(), $dockerSkipped)
        }

        $summary.Trim()
    } | Out-Null
}

# 5. Dependency audit ---------------------------------------------------------
Invoke-Checked -Check 'dotnet list package --vulnerable' -Action {
    $audit = Invoke-Native -FilePath 'dotnet' -Arguments @(
        'list', 'API.sln', 'package', '--vulnerable', '--include-transitive'
    )

    if ($audit.ExitCode -ne 0) {
        throw 'dotnet list package failed (offline or NuGet unreachable).'
    }

    $vulnerable = @($audit.Output -split "`r?`n" | Where-Object { $_ -match 'has the following vulnerable packages' })
    if ($vulnerable.Count -gt 0) {
        throw ('vulnerable packages reported for: ' + (($vulnerable | ForEach-Object { $_.Trim() }) -join '; '))
    }

    $projects = @($audit.Output -split "`r?`n" | Where-Object { $_ -match 'no vulnerable packages' })
    ("no vulnerable packages ({0} project report(s))" -f $projects.Count)
} | Out-Null

# 6+7. Secret scanning --------------------------------------------------------
$gitleaks = $null
try {
    $gitleaks = Resolve-Gitleaks
}
catch {
    Add-Result -Check 'gitleaks availability' -Status 'FAIL' -Detail $_.Exception.Message
}

if (-not $gitleaks) {
    Add-Result -Check 'gitleaks working tree' -Status 'SKIPPED' -Detail 'gitleaks not available (-NoDownload)'
    Add-Result -Check 'gitleaks git history' -Status 'SKIPPED' -Detail 'gitleaks not available (-NoDownload)'
}
else {
    $workingTreeReport = Join-Path ([System.IO.Path]::GetTempPath()) ('gitleaks-dir-' + [guid]::NewGuid().ToString('N') + '.json')
    Invoke-Checked -Check 'gitleaks working tree' -Action {
        $export = New-CommitCandidateExport
        try {
            $count = Invoke-Gitleaks -Executable $gitleaks -Arguments @('dir', $export) -ReportPath $workingTreeReport
            if ($count -gt 0) {
                throw "$count potential secret(s) in files that would be committed; run 'gitleaks dir .' for the list."
            }
            "no findings in commit-candidate files (gitleaks v$gitleaksVersion)"
        }
        finally {
            Remove-Item -LiteralPath $export -Recurse -Force -ErrorAction SilentlyContinue
        }
    } | Out-Null

    $historyReport = Join-Path ([System.IO.Path]::GetTempPath()) ('gitleaks-git-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        $historyCount = Invoke-Gitleaks -Executable $gitleaks -Arguments @('git', '--log-opts=--all', '.') -ReportPath $historyReport
        if ($historyCount -eq 0) {
            Add-Result -Check 'gitleaks git history' -Status 'PASS' -Detail 'no findings in history'
        }
        elseif ($FailOnHistoricalSecrets) {
            Add-Result -Check 'gitleaks git history' -Status 'FAIL' -Detail "$historyCount historical finding(s); SEC-01 history rewrite is required"
        }
        else {
            Add-Result -Check 'gitleaks git history' -Status 'PENDING' -Detail "$historyCount historical finding(s) - SEC-01 rotation + history rewrite outstanding"
        }
    }
    catch {
        Add-Result -Check 'gitleaks git history' -Status 'FAIL' -Detail $_.Exception.Message
    }
}

# Summary ---------------------------------------------------------------------
Write-Host ''
Write-Host '== Summary ==' -ForegroundColor Cyan
$results | Format-Table -AutoSize -Wrap | Out-String | Write-Host

$failed = @($results | Where-Object { $_.Status -eq 'FAIL' })
$pending = @($results | Where-Object { $_.Status -eq 'PENDING' })

if ($failed.Count -gt 0) {
    Write-Host ("VERIFICATION FAILED: {0} check(s) failed." -f $failed.Count) -ForegroundColor Red
    exit 1
}

if ($pending.Count -gt 0) {
    Write-Host ("VERIFICATION PASSED with {0} pending item(s): SEC-01 credential rotation and Git history rewrite." -f $pending.Count) -ForegroundColor Yellow
    exit 0
}

Write-Host 'VERIFICATION PASSED.' -ForegroundColor Green
exit 0
