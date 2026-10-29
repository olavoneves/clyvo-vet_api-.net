# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/);
versões em [SemVer](https://semver.org/lang/pt-BR/). Cada versão tem tag no Git.

## [1.2.1] — 2026-10-29

Acabamento da Sprint 4.

### Adicionado
- `Directory.Build.props` com a versão da solução. O Swagger e o
  `service.version` do OpenTelemetry passam a reportar `1.2.1`; antes caíam no
  `1.0.0` padrão do SDK.
- Cabeçalhos `X-Forwarded-For`, `-Proto` e `-Host` aplicados antes da geração
  de links. Os proxies confiáveis ficam em `Proxy:Confiaveis` (IPs ou CIDR).
  Com a lista vazia, qualquer origem é aceita só em `Development` e `Testing`.
- Exemplos com `_links` no Swagger para metas (item e coleção), resumo de
  obrigações e coorte.
- Testes das migrations sem banco (script `Up`/`Down` e snapshot igual ao
  modelo), dos health checks com as dependências fora do ar e dos repositórios
  de leitura e de metas contra o Oracle local.
- Cobertura publicada em dois modos, `docs/cobertura/sem-infraestrutura` e
  `docs/cobertura/com-infraestrutura`. O script aborta se o host do Oracle for
  da FIAP.

### Alterado
- README: o insights não tem deploy; passo a passo do compose com o `.env`.
- `scripts/cobertura.sh` usa caminhos relativos, que funcionam também no Git
  Bash do Windows.

## [1.2.0] — 2026-10-28

Sprint 4: listagem paginada, HATEOAS, testes e cobertura.

### Adicionado
- `GET /api/insights/obrigacoes` sobre a `VW_CLV_OBRIGACAO_DETALHE`, com
  paginação, filtros (status, grupo, categoria, período e pet) e ordenação por
  lista fechada. `GET /api/insights/obrigacoes/{id}` responde 404 para obrigação
  de outra clínica.
- `_links` absolutos em todos os GET e nas respostas de POST e PUT, montados por
  um único `GeradorDeLinks`. Novo `GET /api/insights/metas/{id}`.
- Migration `MapeiaVwObrigacaoDetalhe`, sem DDL, que só registra a view no
  modelo.
- Testes de listagem, critérios, links e HATEOAS; cobertura com coverlet e
  reportgenerator; README com diagramas Mermaid e mapa da ementa.

### Alterado
- `GET /api/insights/metas` passa a devolver `{ itens, _links }` em vez de um
  array. **Quebra de contrato**; nenhum cliente consumia o endpoint.

### Corrigido
- Erros saem com `Content-Type: application/problem+json`.

### Segurança
- A credencial do Oracle sai do `appsettings.json`, e as senhas saem do
  `launchSettings.json`, do `docker-compose.yaml` (agora lidas do `.env`) e do
  README. A API não sobe sem `ConnectionStrings__Oracle`. O histórico do Git
  não foi reescrito.

## [1.1.1] — 2026-09-08

### Corrigido
- A imagem de execução não instala mais o `curl`, e o build depende só do
  restore do NuGet. O healthcheck do compose foi removido junto; `GET /health`
  é sondado de fora do container.

## [1.1.0] — 2026-09-08

Rodada de conformidade da Sprint 3.

### Adicionado
- Health check do clyvo-core como dependência externa, que degrada em vez de
  derrubar.
- Correlação por requisição alinhada ao TraceId, e log também em arquivo.
- `/metrics` com duração por rota e taxa de erro.
- Teste que trava a taxa derivada contra o `PC_CUMPRIMENTO` da view.
- 8 testes contra Oracle e MongoDB reais, protegidos por
  `CLYVO_TESTES_DE_INFRA`.

### Alterado
- Porta de leitura renomeada para `ILeituraDoCoreRepository`.
- Testes no padrão `MetodoTestado_Cenario_ResultadoEsperado`.

## [1.0.0] — 2026-09-08

Primeira versão: análise de coorte em Clean Architecture sobre as views do
core, JWT compartilhado, CRUD de metas com migrations EF, snapshots no Oracle e
no MongoDB, e observabilidade com Serilog, OpenTelemetry e health checks.
