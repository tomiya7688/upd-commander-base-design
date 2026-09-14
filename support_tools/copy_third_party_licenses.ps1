param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("roslyn", "llvm")]
    [string]$Component,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$noticeSource = Join-Path $repositoryRoot "THIRD_PARTY_NOTICES.md"
$licenseName = switch ($Component) {
    "roslyn" { "ROSLYN-LICENSE.txt" }
    "llvm" { "LLVM-LICENSE.txt" }
}
$licenseSource = Join-Path $repositoryRoot (Join-Path "licenses" $licenseName)

if (-not (Test-Path -LiteralPath $noticeSource -PathType Leaf)) {
    throw "Third-party notice not found: $noticeSource"
}
if (-not (Test-Path -LiteralPath $licenseSource -PathType Leaf)) {
    throw "Third-party license not found: $licenseSource"
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputDirectory)
$licenseOutput = Join-Path $resolvedOutput "licenses"
New-Item -ItemType Directory -Path $licenseOutput -Force | Out-Null
Copy-Item -LiteralPath $noticeSource -Destination (Join-Path $resolvedOutput "THIRD_PARTY_NOTICES.md") -Force
Copy-Item -LiteralPath $licenseSource -Destination (Join-Path $licenseOutput $licenseName) -Force
