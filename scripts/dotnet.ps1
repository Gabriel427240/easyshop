param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
$ErrorActionPreference = 'Stop'
$installed = Get-Command dotnet -ErrorAction SilentlyContinue
$localSdk = Join-Path $env:LOCALAPPDATA 'EasyShop/dotnet/dotnet.exe'
if ($installed) { & $installed.Source @Arguments }
elseif (Test-Path -LiteralPath $localSdk) { & $localSdk @Arguments }
else { throw 'Instale o SDK .NET 10: https://dotnet.microsoft.com/download/dotnet/10.0' }
exit $LASTEXITCODE
