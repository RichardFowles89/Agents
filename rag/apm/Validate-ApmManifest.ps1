param(
    [string]$ManifestPath = "./package.manifest.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -Path $ManifestPath)) {
    throw "Manifest not found: $ManifestPath"
}

$content = Get-Content -Path $ManifestPath -Raw
if ([string]::IsNullOrWhiteSpace($content)) {
    throw "Manifest is empty: $ManifestPath"
}

$manifest = $content | ConvertFrom-Json

$requiredTop = @("package", "runtime", "compatibility", "tools", "environment", "operational")
foreach ($key in $requiredTop) {
    if (-not $manifest.PSObject.Properties.Name.Contains($key)) {
        throw "Missing top-level key: $key"
    }
}

if ([string]::IsNullOrWhiteSpace($manifest.package.name)) {
    throw "package.name is required"
}

if ([string]::IsNullOrWhiteSpace($manifest.package.version)) {
    throw "package.version is required"
}

if ($manifest.tools.Count -lt 1) {
    throw "At least one tool must be defined"
}

$toolNames = @{}
foreach ($tool in $manifest.tools) {
    if ([string]::IsNullOrWhiteSpace($tool.name)) {
        throw "All tools must have a non-empty name"
    }

    if ($toolNames.ContainsKey($tool.name)) {
        throw "Duplicate tool name: $($tool.name)"
    }

    $toolNames[$tool.name] = $true
}

Write-Host "APM manifest validation passed: $ManifestPath"
