param([string]$Serial='emulator-5554')
$ErrorActionPreference='Stop'
$env:PYTHONIOENCODING='utf-8'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
python -c "import PIL, imageio_ffmpeg"
if ($LASTEXITCODE -ne 0) { throw 'Instale as dependências: python -m pip install Pillow imageio-ffmpeg' }
$androidSdk=Join-Path $env:LOCALAPPDATA 'Android/Sdk'
$javaCommand=Get-Command java -ErrorAction Stop
$demoJava=Split-Path (Split-Path $javaCommand.Source -Parent) -Parent
& ./scripts/dotnet.ps1 build src/EasyShop.Android -c Debug --output artifacts/android-demo '-p:ApplicationId=br.edu.easyshop.demo' '-p:ApplicationTitle=EasyShop Demo' "-p:AndroidSdkDirectory=$androidSdk" "-p:JavaSdkDirectory=$demoJava"
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o APK Demo.' }
adb -s $Serial install -r artifacts/android-demo/br.edu.easyshop.demo-Signed.apk
if ($LASTEXITCODE -ne 0) { throw 'Falha ao instalar. Confira o emulador com adb devices.' }
python ./scripts/gravar-demonstracao.py --serial $Serial
if ($LASTEXITCODE -ne 0) { throw 'A demonstração parou. Confira a saída e a captura falha.png na pasta gerada.' }
