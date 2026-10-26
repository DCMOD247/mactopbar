$ErrorActionPreference = 'Stop'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\MacTopBar'
$shortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\MacTopBar.lnk'
Get-Process MacTopBar -ErrorAction SilentlyContinue | Stop-Process -Force
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Remove-ItemProperty -Path $runKey -Name 'MacTopBar' -ErrorAction SilentlyContinue
Remove-Item $shortcutPath -Force -ErrorAction SilentlyContinue
Remove-Item $installDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host 'MacTopBar has been removed. Preferences in %APPDATA%\MacTopBar were kept.' -ForegroundColor Green
