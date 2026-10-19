<#
.SYNOPSIS
    Enables the repository's versioned git hooks (.githooks) for this clone.

.DESCRIPTION
    Sets `core.hooksPath` to `.githooks` so the pre-commit secret scan
    (.githooks/pre-commit) runs on every commit. The hook uses gitleaks when it
    is installed and falls back to scripts/security/scan-secrets.ps1.

    This is the framework-free alternative to `pre-commit install`
    (.pre-commit-config.yaml is still supported for developers who use it).

.PARAMETER Uninstall
    Restore git's default hook path (.git/hooks).

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/install-git-hooks.ps1
#>
[CmdletBinding()]
param(
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

if (-not (Test-Path -LiteralPath '.git')) {
    throw 'Run this script from a clone of the Mechanic.Api repository.'
}

if ($Uninstall) {
    & git config --unset core.hooksPath
    Write-Host 'git hooks path reset to the default (.git/hooks).'
    exit 0
}

$hookPath = '.githooks/pre-commit'
if (-not (Test-Path -LiteralPath $hookPath)) {
    throw "Missing $hookPath."
}

& git config core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to set core.hooksPath.'
}

$configured = (& git config --get core.hooksPath).Trim()

Write-Host "core.hooksPath = $configured"
Write-Host ''
Write-Host 'The pre-commit hook now scans staged changes for secrets.'
Write-Host 'Scanner used:'
if (Get-Command gitleaks -ErrorAction SilentlyContinue) {
    Write-Host '  gitleaks (installed) with .gitleaks.toml'
}
else {
    Write-Host '  scripts/security/scan-secrets.ps1 (fallback; install gitleaks for full coverage)'
}
Write-Host ''
Write-Host 'Full verification: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1'
