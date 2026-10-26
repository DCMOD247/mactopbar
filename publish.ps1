param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$RuntimeIdentifier = 'win-x64'
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$output = Join-Path $PSScriptRoot "publish\$RuntimeIdentifier"
Write-Host "Publishing standalone MacTopBar for $RuntimeIdentifier..." -ForegroundColor Cyan
dotnet publish .\MacTopBar.csproj --configuration Release --runtime $RuntimeIdentifier --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    --output $output
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE" }
Write-Host "Published: $output\MacTopBar.exe" -ForegroundColor Green
