# Android e emulador

O EasyShop Android é um aplicativo nativo C#/.NET for Android, sem WebView nem servidor externo. Reutiliza por links de compilação as entidades, serviços e repositórios da versão desktop. LiteDB armazena produtos e Microsoft.Data.Sqlite armazena clientes em arquivos privados do aplicativo.

## Ambiente

- SDK .NET 10 e workload `android` (`dotnet workload install android`).
- SDK Android: plataforma API 36, build-tools 36 e platform-tools.
- JDK compatível com o workload; neste computador, JDK Adoptium 17.0.20, workload Android 36.1.2.
- Variáveis `ANDROID_HOME` e `JAVA_HOME` ou Java no PATH. O script também procura SDK em `%LOCALAPPDATA%/Android/Sdk`.
- AVD utilizado: `Eixo_API36_Play`, Android 16/API 36, x86_64, emulador Android oficial com aceleração WHPX.

Referências oficiais de instalação: https://learn.microsoft.com/dotnet/android/getting-started/installation/net-android e https://learn.microsoft.com/dotnet/android/getting-started/installation/dependencies.

## Compilar instalar e executar

```powershell
./scripts/dotnet.ps1 workload install android
./scripts/build-android.ps1
emulator -avd Eixo_API36_Play
adb devices
./scripts/install-android.ps1
```

Use o nome do AVD existente no seu computador (`emulator -list-avds`). Aguarde `adb shell getprop sys.boot_completed` retornar `1`. O APK é `artifacts/android/br.edu.easyshop-Signed.apk`. A compilação Debug inclui as assemblies no APK e não depende de deploy rápido pelo VS Code. Não mova somente DLLs para o Android.

É possível arrastar o APK para a janela do emulador. Para instalar manualmente: `adb -e install -r artifacts/android/br.edu.easyshop-Signed.apk`. O pacote é `br.edu.easyshop`. O APK contém x86_64 e arm64; API mínima declarada: 26/Android 8. A validação real cobre exclusivamente os ambientes registrados nas evidências; isso não comprova compatibilidade com todos os aparelhos ou versões.

## Dados e uso offline

Produtos e clientes ficam em `FilesDir/products.db` e `FilesDir/customers.sqlite` no sandbox Android. Não se conectam ao banco do Windows. O aplicativo não solicita acesso à internet e funciona sem o servidor desktop. Reiniciar ou forçar parada preserva os dados; desinstalar ou limpar armazenamento apaga os dados. Backups automáticos estão desabilitados para evitar cópia não intencional dos cadastros.

## Reproduzir a verificação

Com APK instalado em emulador de demonstração e sem cadastros, execute `python scripts/android-smoke.py`. O script não limpa dados, cadastra exemplos fictícios, testa erros, força parada, reabre e verifica persistência. Não o execute em uma base de uso real. As capturas e o resultado ficam em `docs/evidencias/android/`.

Se houver vários dispositivos, use o serial com `adb -s SERIAL`. Os scripts desta entrega usam `adb -e` e esperam somente um emulador conectado. Falha `INSTALL_FAILED_UPDATE_INCOMPATIBLE` indica assinatura diferente de uma instalação anterior: preserve dados necessários antes de qualquer desinstalação. Para crash, consulte `adb logcat -d` e filtre por EasyShop/AndroidRuntime.

## Limites da entrega

APK assinado com chave local de desenvolvimento, sem publicação em loja. Não houve aparelho físico disponível na validação. Release com assinatura de produção, migrações de esquema futuras e distribuição em loja não fazem parte da avaliação demonstrável atual.
