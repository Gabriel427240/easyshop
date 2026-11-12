param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
$androidSdk = $env:ANDROID_HOME
if (-not $androidSdk) { $androidSdk = Join-Path $env:LOCALAPPDATA 'Android/Sdk' }
$javaSdk = $env:JAVA_HOME
if (-not $javaSdk) {
    $javaCommand = Get-Command java -ErrorAction Stop
    $javaSdk = Split-Path (Split-Path $javaCommand.Source -Parent) -Parent
}
& ./scripts/dotnet.ps1 build src/EasyShop.Android -c $Configuration --output artifacts/android "-p:AndroidSdkDirectory=$androidSdk" "-p:JavaSdkDirectory=$javaSdk"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output 'APK: artifacts/android/br.edu.easyshop-Signed.apk (assinatura local de desenvolvimento)'
