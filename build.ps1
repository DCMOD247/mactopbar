$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
Write-Host 'Building MacTopBar (Release)...' -ForegroundColor Cyan
dotnet build .\MacTopBar.csproj --configuration Release
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
Write-Host 'Build succeeded. Output: .\bin\Release\net10.0-windows\MacTopBar.exe' -ForegroundColor Green
