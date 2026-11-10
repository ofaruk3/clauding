# Local build of the installer (same steps as the release workflow, without uploading).
#   pwsh scripts/pack.ps1 -Version 0.1.0
# Needs the Velopack CLI: dotnet tool install -g vpk
param([string]$Version = "0.1.0")
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot)
dotnet publish Clauding.csproj -c Release -r win-x64 --self-contained false -p:Version=$Version -o publish
vpk pack -u Clauding -v $Version -p publish -e Clauding.exe -i icon.ico -r win-x64 `
  --packTitle Clauding --packAuthors ofc.works -f net9-x64-desktop -o releases
Write-Host "Installer: releases\Clauding-win-Setup.exe"
