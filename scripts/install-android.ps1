$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
$apk = Join-Path (Get-Location) 'artifacts/android/br.edu.easyshop-Signed.apk'
if (-not (Test-Path -LiteralPath $apk)) { throw 'Execute ./scripts/build-android.ps1 primeiro.' }
adb -e install -r $apk
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
adb -e shell monkey -p br.edu.easyshop 1
