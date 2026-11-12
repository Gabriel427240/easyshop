# Auditoria do briefing EasyShop

Revisão realizada em 12/11/2026 contra o arquivo inicial Briefing_Codex_EasyShop_Atualizado.docx. A solicitação posterior confirmou a necessidade de execução em emulador Android. A versão anterior atendia ao núcleo desktop, mas não possuía alvo Android; essa lacuna motivou a inclusão do projeto nativo.

| Item do documento | Resultado da varredura | Evidência e limite |
| --- | --- | --- |
| Inspecionar workspace e ferramentas | Realizado; workspace original vazio, SDK instalado no perfil do usuário | Decisões no relatório; sem remoção de trabalho anterior |
| Produtos: nome, preço decimal, categorias e atributos | Implementado em ambas as interfaces | Serviços compartilhados; LiteDB; capturas web/Android |
| Clientes: nome completo e e-mail | Implementado em ambas as interfaces | Normalização, SQLite parametrizado e UNIQUE |
| Cadastro e listagem | Implementado para os dois módulos | Testes de UI e evidências de persistência |
| UI, Services e Data/Repositories | Separação presente | Web e Activity Android usam os mesmos arquivos de regras e dados |
| CRUD nos repositórios | Create e Read implementados; Update/Delete não implementados | Edição/exclusão eram opcionais no mesmo briefing; não são alegadas como concluídas |
| Estados vazios, loading, sucesso e erro | Presentes nas duas interfaces | UI web e Android, botões bloqueados durante operações |
| Cultura monetária | Entrada vírgula/ponto sem milhar e exibição pt-BR | Decimal em C#, duas casas, preço positivo |
| Validação e duplicidade | Implementado; revisão acrescentou duas correções | 13 testes: inclui whitespace em nomes e chave de atributo vazia |
| Async, recursos e exceções | Implementado conforme suporte dos drivers | Android usa Task.Run; conexões SQLite descartadas; LiteDB descartado |
| Bancos temporários isolados nos testes | Implementado | Pastas únicas por teste, removidas no Dispose |
| Build, testes e análise estática | Executados localmente | Logs, TRX, dotnet format e consulta NuGet |
| Fluxos reais e persistência | Web automatizada e Android via ADB | Force-stop/reabertura Android e restart do executável Windows |
| Android, APK e emulador | Alvo acrescentado nesta revisão | docs/android.md e evidências Android; aparelho físico não testado |
| Diferentes resoluções | Web 1440 e 390 px; Android 1080x2400 e 720x1280 | Segunda resolução Android com modo avião ativo; compat-results.json |
| Gitignore e ausência de bancos/segredos | Configurados e revisão antes do push | Bancos, bin/obj e artifacts ignorados; PDFs e capturas versionados |
| GitHub público autorizado | Criado e publicado em turno anterior | https://github.com/Gabriel427240/easyshop |
| Convite de colaborador | Pendente | Falta identificador; nenhum convite fictício |
| README e diagramas | Entregues e atualizados com Android | README, arquitetura e comandos reais |
| Relatório, manual e PDFs | Entregues e atualizados após revisão | Docs, capturas reais e PDFs renderizados |
| Custos e cronograma original | Valores/sprints não fornecidos | Sem nuvem contratada; custos pessoais e cronograma original a confirmar |
| Roteiro e checklist 3 a 5 minutos | Entregues; roteiro de 4 minutos | docs/video/roteiro.md; gravação e upload são do colaborador |
| Link final do vídeo | Pendente | Não foi gravado nem publicado pelo Codex |
| Enunciado de logística/finanças | Pendente de confirmação | Módulos não aplicáveis ao EasyShop; não foram implementados ou marcados concluídos |
| Prazo final | 30/11/2026 | Aluno deve confirmar os links e finalizar vídeo antes da submissão |

As evidências de teste Android devem ser lidas com o modelo, API e ABI registrados em `evidencias/android/results.json`. Não é possível garantir funcionamento em qualquer emulador ou aparelho sem testá-lo.
