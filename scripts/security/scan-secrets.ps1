<#
.SYNOPSIS
    Dependency-free secret scanner used as the fallback git pre-commit hook.

.DESCRIPTION
    Gitleaks is the primary scanner (see .gitleaks.toml, CI and
    scripts/security/verify-security.ps1). This script exists so the pre-commit
    hook still blocks the project's known secret shapes on machines where the
    gitleaks binary is not installed (gitleaks is distributed as a Go binary).

    It only reports file/line/rule - never the secret value itself - so the
    output can be pasted into a terminal or an issue without leaking anything.

.PARAMETER Staged
    Scan the staged diff (what the pending commit would contain).

.PARAMETER All
    Scan every tracked file (slower; used by verify-security.ps1).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/scan-secrets.ps1 -Staged
#>
[CmdletBinding()]
param(
    [switch]$Staged,
    [switch]$All
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $Staged -and -not $All) {
    throw 'Specify -Staged (pre-commit) or -All (tracked files).'
}

# Paths that intentionally contain example patterns (documentation, tests of the
# scanners themselves) or that are never committed.
$excludedPathPatterns = @(
    '^skills/'
    '^Tests/'
    '^docs/'
    '^\.workbuddy-ai/'
    '^\.vs/'
    '^\.dsh-temp/'
    '^Logs?/'
    '^keys/'
    '(^|/)(bin|obj|node_modules)/'
    '^\.env$'
    '^SECURITY-AUDIT-REPORT\.md$'
    '^\.gitleaks\.toml$'
)

$binaryExtensions = @(
    '.dll', '.exe', '.pdb', '.so', '.dylib', '.zip', '.gz', '.tar', '.7z',
    '.png', '.jpg', '.jpeg', '.gif', '.webp', '.ico', '.pdf', '.woff', '.woff2',
    '.ttf', '.eot', '.mp4', '.mp3', '.suo', '.dtbcache.v2'
)

# Patterns mirror the project rules in .gitleaks.toml (RE2 -> .NET compatible).
$rules = @(
    @{ Id = 'mechanic-jwt-signing-key'; Pattern = 'Breaking[0-9]+Bad' }
    @{ Id = 'mechanic-kavenegar-api-key'; Pattern = '(?i)kavenegar[^\n]{0,80}["''][0-9A-Fa-f]{32,}["'']' }
    @{ Id = 'mechanic-arvancloud-keys'; Pattern = '(?i)(?:access|secret)[-_]?key\s*[:=]\s*["'']?[0-9a-f]{16,}["'']?' }
    @{ Id = 'mechanic-database-password'; Pattern = '(?i)(?:user\s*id|username)=[^;"]{1,64};\s*password=[^;"]{3,}' }
    @{ Id = 'mechanic-postgres-uri'; Pattern = 'postgres(?:ql)?://[^\s:/@]+:[^\s@]+@[^\s/]+' }
    @{ Id = 'mechanic-private-key'; Pattern = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----' }
    @{ Id = 'mechanic-production-settings'; Pattern = '(?i)(^|/)appsettings\.(Production|Staging)\.json$' }
    @{ Id = 'mechanic-agent-log-dump'; Pattern = '(?i)(^|/)otp\.json$' }
)

$findings = New-Object System.Collections.Generic.List[object]

function Test-Excluded {
    param([string]$Path)

    foreach ($pattern in $excludedPathPatterns) {
        if ($Path -match $pattern) { return $true }
    }
    return $false
}

function Test-Binary {
    param([string]$Path)

    $extension = [System.IO.Path]::GetExtension($Path).ToLowerInvariant()
    return $binaryExtensions -contains $extension
}

function Add-Findings {
    param(
        [string]$Path,
        [string]$Line,
        [int]$LineNumber
    )

    foreach ($rule in $rules) {
        if ($Line -match $rule.Pattern) {
            $findings.Add([pscustomobject]@{
                Rule = $rule.Id
                File = $Path
                Line = $LineNumber
            })
        }
    }
}

function Invoke-StagedScan {
    # -U0 keeps the diff to changed lines only; --no-color avoids escape codes.
    $diff = & git diff --cached --unified=0 --no-color --no-ext-diff 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'git diff --cached failed; run this script inside the repository.'
    }

    $currentFile = $null
    $newLineNumber = 0

    foreach ($line in $diff) {
        if ($line -match '^\+\+\+ b/(.+)$') {
            $currentFile = $Matches[1]
            continue
        }

        if ($line -match '^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@') {
            $newLineNumber = [int]$Matches[1]
            continue
        }

        if ($null -eq $currentFile) { continue }
        if (Test-Excluded $currentFile) { continue }

        if ($line.StartsWith('+') -and -not $line.StartsWith('+++')) {
            Add-Findings -Path $currentFile -Line $line.Substring(1) -LineNumber $newLineNumber
            $newLineNumber++
        }
    }
}

function Invoke-TrackedFileScan {
    $tracked = & git ls-files
    if ($LASTEXITCODE -ne 0) {
        throw 'git ls-files failed; run this script inside the repository.'
    }

    foreach ($file in $tracked) {
        if (Test-Excluded $file) { continue }
        if (Test-Binary $file) { continue }
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }

        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $file -ErrorAction SilentlyContinue) {
            $lineNumber++
            Add-Findings -Path $file -Line $line -LineNumber $lineNumber
        }
    }
}

if ($Staged) {
    Invoke-StagedScan
} else {
    Invoke-TrackedFileScan
}

if ($findings.Count -eq 0) {
    Write-Host 'scan-secrets: no findings.'
    exit 0
}

Write-Host "scan-secrets: $($findings.Count) potential secret(s) found - commit blocked."
Write-Host 'Values are intentionally not printed. Remove the secret, load it from .env / the secret store, and rotate it if it was already pushed.'
foreach ($finding in $findings) {
    Write-Host ("  [{0}] {1}:{2}" -f $finding.Rule, $finding.File, $finding.Line)
}

exit 1
