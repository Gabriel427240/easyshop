# EasyShop

Aplicação acadêmica para cadastro e listagem de produtos e clientes. Produtos são documentos no **LiteDB**; clientes são registros no **SQLite**. Interface em português com preço em reais, categorias e atributos opcionais.

## Executar no Windows

Pré-requisitos: SDK .NET 10, navegador moderno e internet apenas para restaurar pacotes. O SDK foi instalado neste computador em `%LOCALAPPDATA%/EasyShop/dotnet`; o script encontra essa instalação ou o `dotnet` do PATH.

```powershell
./scripts/dotnet.ps1 restore EasyShop.slnx
./iniciar.ps1
```

Abra **http://127.0.0.1:5080**. Para parar, pressione Ctrl+C no terminal. Em outro computador com SDK no PATH, também funciona `dotnet run --project src/EasyShop`.

## Funcionalidades e regras

- Produtos: nome de 2 a 120 caracteres, preço positivo até R$ 999.999.999,99, máximo de duas casas decimais. No formulário, use `29,90` ou `29.90`, sem separador de milhar. A API recebe número JSON decimal.
- Até 20 categorias e 20 atributos por produto; categorias/chaves com até 60 caracteres e valores até 200. Categorias repetidas são consolidadas. No formulário, atributos são `chave: valor`, um por linha.
- Clientes: nome e sobrenome até 160 caracteres; e-mail até 254 caracteres, validado, sem nome de exibição, normalizado para minúsculas e sem espaços externos. Domínio deve conter ponto. Não há confirmação de propriedade por e-mail.
- E-mail único garantido também por restrição no banco, inclusive para cadastros concorrentes.
- Estados de carregamento, vazio, sucesso e erro. Layout responsivo; dados permanecem após fechar e reabrir.

Edição, exclusão, autenticação, sincronização e exportação estão fora desta entrega. Produtos com o mesmo nome são permitidos. O app é de uso local individual e não deve ser exposto na internet.

## Arquitetura

```mermaid
flowchart LR
 UI[UI HTML CSS JavaScript] --> API[Endpoints ASP.NET Core]
 API --> PS[ProductService]
 API --> CS[CustomerService]
 PS --> PR[ProductRepository]
 CS --> CR[CustomerRepository]
 PR --> L[(LiteDB products.db)]
 CR --> S[(SQLite customers.sqlite)]
```

`src/EasyShop/wwwroot`: interface; `Models`: entidades; `Services`: validações; `Data`: repositórios e conexões. Dependências injetadas pelo ASP.NET Core. LiteDB oferece API síncrona, com instância singleton descartada no encerramento. SQLite usa conexões por operação, comandos parametrizados e API assíncrona; o driver pode executar I/O de forma síncrona internamente.

## Dados locais e configuração

Por padrão: `%LOCALAPPDATA%/EasyShop/data/products.db` e `customers.sqlite`. Nenhum banco é versionado. Para isolar uma demonstração:

```powershell
./scripts/dotnet.ps1 run --project src/EasyShop -- --DataDirectory=C:/Temp/EasyShopDemo
```

Também é possível configurar a variável `DataDirectory`. Caminhos relativos são resolvidos a partir do diretório de trabalho do processo. Para backup, encerre o aplicativo e copie a pasta de dados. Os dados não são criptografados; proteja a conta do Windows e não use informações pessoais reais nas demonstrações.

## Build e testes

```powershell
./scripts/dotnet.ps1 build EasyShop.slnx -c Release
./scripts/dotnet.ps1 test EasyShop.slnx -c Release
./scripts/dotnet.ps1 format EasyShop.slnx --verify-no-changes --no-restore
./scripts/dotnet.ps1 list src/EasyShop package --include-transitive --vulnerable
```

Os testes usam pastas temporárias distintas para validações, reabertura dos bancos, normalização e duplicidade concorrente. Evidências em [docs/evidencias](docs/evidencias). O teste de interface está em `scripts/ui-smoke.cjs`: instale Playwright em uma pasta temporária, instale Chromium com a CLI e configure `NODE_PATH` para essa pasta. Execute com uma base vazia de demonstração e o app rodando na porta 5080. O script cadastra dados fictícios e gera capturas; não o execute contra dados de uso real.

## Empacotamento real

Alvo: servidor local ASP.NET Core para Windows x64 com UI no navegador. **Não é APK e não tem alvo Android.** Não foi fornecida exigência específica de plataforma; a escolha atende LiteDB e SQLite sem workloads móveis. Se Android for obrigatório, será necessária uma adaptação, ainda não concluída.

```powershell
./scripts/dotnet.ps1 publish src/EasyShop -c Release -r win-x64 --self-contained true --output artifacts/publish/win-x64
```

Execute `EasyShop.exe` dentro dessa pasta e abra a URL local. Preserve todos os arquivos, incluindo `wwwroot` e bibliotecas nativas. A saída `artifacts` não é versionada; o executável inclui runtime e dispensa instalar o SDK na máquina de destino.

## Problemas comuns

- SDK ausente: instale [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), reabra o terminal e execute o script.
- Scripts bloqueados: use `powershell -ExecutionPolicy Bypass -File ./iniciar.ps1` somente para executar este script inspecionado; não é preciso alterar a política global.
- Porta em uso: feche a outra instância EasyShop. O app escuta exclusivamente em `127.0.0.1:5080`.
- Falha no banco: confira permissões da pasta, espaço em disco e se outra instância mantém o LiteDB aberto. Consulte o terminal; a UI omite detalhes internos.
- Falha no restore: confira conexão e acesso a `api.nuget.org`.
- Preço inválido: digite `1299,90`, sem `R$` e sem ponto de milhar.

## Entrega e colaboração

- [Relatório executivo](docs/relatorio-executivo.md)
- [Manual do usuário](docs/manual-do-usuario.md)
- [Roteiro e checklist do vídeo](docs/video/roteiro.md)
- [Checklist e links finais](docs/entrega.md)
- [Licenças de dependências](docs/dependencias.md)

O aluno supervisiona a entrega. O colaborador grava, narra, edita e publica o vídeo de 3 a 5 minutos; seu identificador GitHub ainda precisa ser informado. Não há vídeo publicado por esta implementação. Os módulos de logística/finanças estão **não aplicáveis ao escopo EasyShop - pendentes de confirmação**.

Prazo informado no briefing: **30/11/2026**. Consulte o checklist para as pendências acadêmicas e os resultados efetivamente verificados.
