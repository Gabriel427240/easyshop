# Roteiro do vídeo EasyShop

Duração prevista para o roteiro manual: **4 minutos**. A [automação de demonstração](automacao.md) já gera uma gravação local com narração sintética, legendas e verificações reais no emulador. A publicação e o link público continuam a confirmar. O roteiro abaixo é uma opção para gravação e narração próprias.

| Tempo | Cena | Narração sugerida |
| --- | --- | --- |
| 0:00-0:25 | Abrir APK no emulador | O EasyShop organiza produtos e clientes com persistência local em dois bancos: LiteDB e SQLite. Mostrar app Android aberto; versão web é alternativa desktop. |
| 0:25-0:55 | Pastas no VS Code | A UI chama endpoints, que usam serviços para validar dados e repositórios para persistir. Mostrar Models, Services, Data e wwwroot. |
| 0:55-1:45 | Produtos | Cadastrar Caderno pontilhado por 29,90, categorias Papelaria e Escritório, atributos Cor: Verde e Tamanho: A5. Mostrar listagem e preço formatado. Tentar preço zero e explicar validação. |
| 1:45-2:35 | Clientes | Cadastrar Marina Exemplo, marina@example.com. Mostrar normalização e tentar repetir o e-mail. Explicar restrição única no SQLite. |
| 2:35-3:05 | Reiniciar aplicativo Android | Forçar parada e reabrir o EasyShop sem limpar armazenamento. Mostrar que produtos e clientes continuam listados. |
| 3:05-3:35 | Testes e documentação | Executar testes; mostrar resultado real, evidências do emulador, README e manual. Não alegar teste em aparelho físico. |
| 3:35-4:00 | Escopo e entrega | Mostrar link real do repositório e APK. Explicar dados locais no Android e opção web; logística/finanças não integram o escopo confirmado. Dizer que o teste foi em emulador, não em aparelho físico. |

## Checklist de gravação

- [ ] Instalar APK validado em emulador exclusivo para a gravação e usar base sem dados pessoais. Alternativa desktop: base isolada (`--DataDirectory=C:/Temp/EasyShopVideo`).
- [ ] Usar somente dados fictícios acima; não exibir tokens, e-mails pessoais, notificações ou arquivos privados.
- [ ] Capturar em 1920×1080 ou 1280×720, texto legível, áudio testado e cursor visível.
- [ ] Gravar com OBS, ferramenta de captura do Windows ou equivalente; conferir começo e fim antes de editar.
- [ ] Demonstrar cadastro, listagem, erro e reinício com a mesma pasta de dados.
- [ ] Manter duração entre 3 e 5 minutos e revisar áudio e legibilidade.
- [ ] Colaborador publicar no YouTube/Drive/Loom com acesso adequado à avaliação.
- [ ] Abrir o link em janela anônima para verificar acesso, sem expor dados privados.
- [ ] Inserir link final em `docs/entrega.md` e neste roteiro antes de 30/11/2026.

Não adaptar este vídeo para caixa, viagens ou dashboards sem confirmação de que outro enunciado se aplica.
