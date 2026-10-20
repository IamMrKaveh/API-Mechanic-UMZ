<#
.SYNOPSIS
    Rewrites the Git history to remove the secrets leaked before SEC-01 was fixed.

.DESCRIPTION
    SEC-01 of the security audit: otp.json (an agent log that embedded the
    production appsettings) and environment-specific settings files were
    committed, and several historical source/appsettings files contained live
    credentials. Rotating the credentials is mandatory (they must be considered
    compromised); this script removes the values from history as well.

    What it does:
      * removes file-level leaks: otp.json, Presentation/appsettings.Production.json,
        MainApi/.env and the legacy MainApi/appsettings.json,
      * redacts the remaining leaked values with `git filter-repo --replace-text`,
        generating the replacement list from a gitleaks report (values are never
        printed by this script),
      * rewrites all reachable commits and tags.

    DESTRUCTIVE: every commit hash changes, the remote is detached by filter-repo,
    and every collaborator must re-clone. It runs as a dry-run unless -Execute is
    passed, and creates a backup bundle by default.

    Prerequisites: `git filter-repo` (pip install git-filter-repo / winget install
    GitHub.git-filter-repo). On Windows the pip module is used through
    `python -m git_filter_repo` when the `git filter-repo` alias is unavailable.
    Run it only after rotating the credentials.

.PARAMETER Execute
    Perform the rewrite. Without this switch the script only prints the plan.

.PARAMETER ReplacementsPath
    Use an existing git filter-repo replacements file instead of generating one.

.PARAMETER SkipBackup
    Do not create the safety bundle (not recommended).

.PARAMETER GitleaksPath
    Path to a gitleaks executable used to build the replacement list.

.PARAMETER PythonPath
    Python interpreter used for the `-m git_filter_repo` fallback when
    `git filter-repo` is not available on PATH (Windows-friendly).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/purge-git-history.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/purge-git-history.ps1 -Execute
#>
[CmdletBinding()]
param(
    [switch]$Execute,
    [string]$ReplacementsPath,
    [switch]$SkipBackup,
    [string]$GitleaksPath,
    [string]$PythonPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

# File-level leaks. The last three are paths from the pre-audit project layout that
# no longer exist in HEAD; removing them keeps legacy connection strings out of history.
$pathsToRemove = @(
    'otp.json'
    'Presentation/appsettings.Production.json'
    'MainApi/.env'
    'MainApi/appsettings.json'
    'DataAccessLayer/MechanicContext.cs'
)

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

    [pscustomobject]@{ Output = ($lines -join [Environment]::NewLine); ExitCode = $exitCode }
}

Write-Host ''
Write-Host '== SEC-01 Git history rewrite ==' -ForegroundColor Cyan
Write-Host 'This changes every commit hash. Rotate the credentials BEFORE running it.'
Write-Host ''

if (-not (Test-Path -LiteralPath '.git')) {
    throw 'Run this script inside the repository clone.'
}

# Resolve git-filter-repo -----------------------------------------------------
# Preferred: `git filter-repo` on PATH. Windows fallback: the Python module
# (`pip install git-filter-repo` provides `python -m git_filter_repo`), because
# Git for Windows does not always resolve a bare `git-filter-repo` script.
function Resolve-FilterRepo {
    param([string]$Python)

    $direct = Invoke-Native -FilePath 'git' -Arguments @('filter-repo', '--version')
    if ($direct.ExitCode -eq 0) {
        return [pscustomobject]@{ File = 'git'; Prefix = @('filter-repo') }
    }

    $candidates = @()
    if ($Python) { $candidates += $Python }
    foreach ($name in @('python', 'python3', 'py')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($command) { $candidates += $command.Source }
    }

    foreach ($candidate in $candidates) {
        $module = Invoke-Native -FilePath $candidate -Arguments @('-m', 'git_filter_repo', '--version')
        if ($module.ExitCode -eq 0) {
            return [pscustomobject]@{ File = $candidate; Prefix = @('-m', 'git_filter_repo') }
        }
    }

    return $null
}

$filterRepoCommand = Resolve-FilterRepo -Python $PythonPath
if ($null -eq $filterRepoCommand) {
    Write-Host 'git filter-repo is not installed.' -ForegroundColor Red
    Write-Host '  pip install git-filter-repo        # provides python -m git_filter_repo'
    Write-Host '  or: winget install GitHub.git-filter-repo'
    Write-Host 'Then re-run, optionally with -PythonPath <python.exe>.'
    Write-Host 'See docs/07-operations/security-verification.md for the full runbook.'
    exit 2
}

$status = Invoke-Native -FilePath 'git' -Arguments @('status', '--porcelain')
if ($status.Output.Trim().Length -gt 0) {
    throw 'The working tree is not clean. Commit or stash your changes first (a rewrite needs a clean state).'
}

# Build the redaction list ----------------------------------------------------
$temporaryReplacements = $null
if (-not $ReplacementsPath) {
    $gitleaks = $GitleaksPath
    if (-not $gitleaks) {
        $command = Get-Command gitleaks -ErrorAction SilentlyContinue
        if ($command) { $gitleaks = $command.Source }
    }

    if (-not $gitleaks) {
        Write-Host 'gitleaks was not found, so only file-level leaks can be removed.' -ForegroundColor Yellow
        Write-Host 'Install gitleaks (or pass -GitleaksPath) to also redact the leaked values.'
        $ReplacementsPath = $null
    }
    else {
        $reportPath = Join-Path ([System.IO.Path]::GetTempPath()) ('gitleaks-rewrite-' + [guid]::NewGuid().ToString('N') + '.json')
        $scan = Invoke-Native -FilePath $gitleaks -Arguments @(
            'git', '--log-opts=--all', '--config', '.gitleaks.toml',
            '--report-format', 'json', '--report-path', $reportPath,
            '--no-banner', '.'
        )

        if (-not (Test-Path -LiteralPath $reportPath)) {
            throw 'gitleaks produced no report; cannot build the redaction list.'
        }

        $findings = @()
        $rawReport = Get-Content -LiteralPath $reportPath -Raw
        Remove-Item -LiteralPath $reportPath -Force -ErrorAction SilentlyContinue
        if ($rawReport -and $rawReport.Trim().Length -gt 2) {
            $findings = @($rawReport | ConvertFrom-Json)
        }

        $secrets = @()
        foreach ($finding in $findings) {
            if ($finding.Secret -and $finding.Secret.Length -ge 8) { $secrets += $finding.Secret }
        }
        $secrets = $secrets | Sort-Object -Unique

        $temporaryReplacements = Join-Path ([System.IO.Path]::GetTempPath()) ('filter-repo-replacements-' + [guid]::NewGuid().ToString('N') + '.txt')
        $lines = New-Object System.Collections.Generic.List[string]
        foreach ($secret in $secrets) {
            # Use *literal* replacements: regex-escaping a secret makes filter-repo's
            # Python regex engine interpret the backslashes and silently fail to match.
            if ($secret.Length -lt 8) { continue }
            if ($secret.Contains('==>') -or $secret.Contains("`n") -or $secret.Contains("`r")) { continue }
            $lines.Add($secret + '==>***SEC-01-REMOVED***')
        }
        # Catch variants of the leaked signing key that a partial secret may have missed.
        $lines.Add('regex:Breaking[0-9]+Bad[A-Za-z0-9!@#$%^&*()+_\-]*==>***SEC-01-REMOVED***')
        Set-Content -LiteralPath $temporaryReplacements -Value $lines -Encoding ASCII
        $ReplacementsPath = $temporaryReplacements
        Write-Host ("Redaction list built from {0} historical secret value(s)." -f $secrets.Count)
    }
}

# Plan ------------------------------------------------------------------------
Write-Host ''
Write-Host 'Plan:'
Write-Host '  remove paths:'
foreach ($path in $pathsToRemove) { Write-Host "    - $path" }
if ($ReplacementsPath) {
    Write-Host ("  redact leaked values using: {0}" -f $ReplacementsPath)
}
Write-Host ''

if (-not $Execute) {
    Write-Host 'Dry run only. Re-run with -Execute to rewrite the history.' -ForegroundColor Yellow
    if ($temporaryReplacements) { Remove-Item -LiteralPath $temporaryReplacements -Force -ErrorAction SilentlyContinue }
    exit 0
}

# Backup ----------------------------------------------------------------------
if (-not $SkipBackup) {
    $backupPath = Join-Path ([System.IO.Path]::GetTempPath()) ('mechanic-pre-rewrite-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.bundle')
    $bundle = Invoke-Native -FilePath 'git' -Arguments @('bundle', 'create', $backupPath, '--all')
    if ($bundle.ExitCode -ne 0) {
        if ($temporaryReplacements) { Remove-Item -LiteralPath $temporaryReplacements -Force -ErrorAction SilentlyContinue }
        throw ('git bundle failed: ' + $bundle.Output.Trim())
    }
    Write-Host ("Backup bundle: {0}" -f $backupPath)
}

# Rewrite ---------------------------------------------------------------------
try {
    $arguments = @()
    $arguments += $filterRepoCommand.Prefix
    $arguments += @('--force', '--invert-paths')
    foreach ($path in $pathsToRemove) { $arguments += @('--path', $path) }
    if ($ReplacementsPath) { $arguments += @('--replace-text', $ReplacementsPath) }

    $rewrite = Invoke-Native -FilePath $filterRepoCommand.File -Arguments $arguments
    if ($rewrite.ExitCode -ne 0) {
        throw ('git filter-repo failed: ' + $rewrite.Output.Trim())
    }
}
finally {
    if ($temporaryReplacements) {
        Remove-Item -LiteralPath $temporaryReplacements -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ''
Write-Host 'History rewritten. Next steps:' -ForegroundColor Cyan
Write-Host '  1. git remote add origin <url>        # filter-repo detaches the remote'
Write-Host '  2. git push --force-with-lease origin main'
Write-Host '  3. every collaborator must re-clone (old clones still contain the secrets)'
Write-Host '  4. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1 -FailOnHistoricalSecrets'
Write-Host '  5. invalidate any credential that was live during the leak, even after the rewrite.'
