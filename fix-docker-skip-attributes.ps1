$testsRoot = "Tests"

$files = Get-ChildItem -Path $testsRoot -Filter *.cs -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    $original = $content

    $content = $content -replace '\[RequiresDockerFact\]', '[Fact]'
    $content = $content -replace '\[RequiresDockerTheory\]', '[Theory]'
    $content = $content -replace '(\r?\n)\s*using Tests\.TestInfrastructure\.Attributes;\s*(\r?\n)', '$1$2'

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "Updated: $($file.FullName)"
    }
}

$attrFile = Join-Path $testsRoot "TestInfrastructure\Attributes\RequiresDockerFactAttribute.cs"
if (Test-Path $attrFile) {
    Remove-Item $attrFile
    Write-Host "Deleted: $attrFile"
}

Write-Host "Done."
