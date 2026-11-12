$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
& ./scripts/dotnet.ps1 run --project src/EasyShop --no-launch-profile
