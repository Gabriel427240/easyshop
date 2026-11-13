# Demonstração automática com voz e legendas

A automação controla o EasyShop real pelo ADB, verifica os resultados e grava a tela do emulador. Depois monta um vídeo em 1920×1080, com a tela Android à esquerda, explicações à direita, narração sintética em português e legendas incorporadas. Também entrega um arquivo SRT editável.

## Executar novamente

Deixe o emulador aberto e execute, na raiz do projeto:

```powershell
./scripts/demonstrar-app.ps1
```

Se o dispositivo tiver outro serial, consulte `adb devices` e use:

```powershell
./scripts/demonstrar-app.ps1 -Serial emulator-5554
```

Requisitos: Python com `Pillow` e `imageio-ffmpeg`, voz Windows `Microsoft Maria Desktop`, SDK .NET com workload Android, JDK compatível e Android SDK. Dependências de vídeo:

```powershell
python -m pip install Pillow imageio-ffmpeg
```

O comando compila uma instalação separada com o identificador `br.edu.easyshop.demo` e o nome **EasyShop Demo**. O código funcional é o mesmo do aplicativo. Os bancos da instalação original `br.edu.easyshop` não são acessados. A automação cria somente cadastros fictícios na instalação Demo; não limpa dados, não desinstala aplicativos e não publica arquivos. Cada execução usa nomes e e-mails próprios, permitindo repetição sem apagar a base.

Não toque no emulador durante a execução. A demonstração demora alguns minutos. Uma falha interrompe a execução e grava `falha.png`; um vídeo concluído só é emitido quando todas as verificações passam.

## Arquivos gerados

Cada execução fica em `artifacts/video/AAAAMMDD-HHMMSS/`:

- `easyshop-demonstracao.mp4`: vídeo final com voz e legendas visíveis.
- `tela-original.mp4`: captura original do emulador, sem narração.
- `legendas.srt`: legendas para edição ou importação em outro editor.
- `narracao.json` e `voz-*.wav`: textos e áudios das cenas.
- `resultado.json`: verificações aprovadas.
- `timeline.json`, capturas PNG e `render.log`: evidências e tempos da gravação.

Para personalizar os textos, edite `SCENES` em `scripts/gravar-demonstracao.py`. A narração é gerada localmente pelo Windows; não requer enviar o texto a um serviço externo.

## O que é verificado

Abertura, digitação com teclado, atributos em múltiplas linhas, cadastro e listagem de produto, preço formatado, rejeição de preço zero, cadastro de cliente, normalização de e-mail, rejeição de e-mail inválido e repetido, preservação de rascunho entre abas e permanência de produtos e clientes após encerrar e reabrir o app. A execução é em emulador Android; não representa um teste em aparelho físico.

Para gravar sua própria voz, use `tela-original.mp4` no editor e importe `legendas.srt` como roteiro. O vídeo final já inclui a voz sintética e pode ser usado diretamente após sua revisão.

## Usar uma voz externa, como ElevenLabs

Instale as dependências de sincronização:

```powershell
python -m pip install -r scripts/requirements-transcricao.txt
```

Primeiro grave a demonstração. Depois informe a pasta dessa gravação e o áudio completo:

```powershell
python scripts/sincronizar-narracao.py --audio "C:/Videos/minha-voz.mp3" --source "artifacts/video/AAAAMMDD-HHMMSS" --output "artifacts/video/minha-voz"
```

O áudio deve seguir os oito parágrafos de `narracao.json`, na mesma ordem. A transcrição roda localmente com Whisper; o modelo `base` é baixado no primeiro uso. O arquivo de voz não é enviado a um serviço externo. Se já houver uma transcrição Whisper com tempos por palavra, use `--transcription caminho/transcricao.json` para reutilizá-la sem executar o reconhecimento novamente.

A sincronização ajusta a duração de cada cena ao parágrafo correspondente, preserva a velocidade da voz, substitui a narração anterior e gera novas legendas. Créditos falados ao final, iniciados por “Desenvolvido por”, também são incluídos. Uma narração muito diferente do roteiro interrompe o processo para revisão.

A pasta de saída contém `EasyShop - Video Completo - ElevenLabs.mp4`, `legendas-elevenlabs.srt`, a transcrição e o relatório de alinhamento. Os arquivos permanecem locais e fora do controle de versão.

## Validação registrada

Em 13/11/2026, a demonstração passou pelas 14 verificações do [relatório Android](../evidencias/android/ux-video-results.json). O vídeo com voz do Windows tem aproximadamente 4min13s. A versão sincronizada com a voz externa fornecida tem aproximadamente 2min46s, incluindo os créditos finais. Os dois MP4 foram decodificados por completo sem erro e revisados visualmente em quadros de amostra. Nenhum vídeo foi publicado em um serviço externo por esta automação.
