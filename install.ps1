$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$source = Join-Path $PSScriptRoot 'publish\win-x64\MacTopBar.exe'
if (-not (Test-Path $source)) {
    throw 'The published EXE was not found. Run .\publish.ps1 first.'
}
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\MacTopBar'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item $source (Join-Path $installDir 'MacTopBar.exe') -Force
$shortcutPath = Join-Path $startMenuDir 'MacTopBar.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $installDir 'MacTopBar.exe'
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = 'macOS-inspired menu bar for Windows'
$shortcut.Save()
Write-Host "Installed to $installDir" -ForegroundColor Green
Write-Host 'A MacTopBar shortcut was added to the Start Menu.'
$launch = Read-Host 'Launch MacTopBar now? [Y/N]'
if ($launch -match '^(Y|YES)$') { Start-Process (Join-Path $installDir 'MacTopBar.exe') }
