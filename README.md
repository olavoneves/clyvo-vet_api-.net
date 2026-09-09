# Clyvo Insights — serviço .NET de leitura e análise

Serviço ASP.NET Core 8 do ecossistema **Clyvo Vet**. Ele é o lado de *leitura* e
existe para responder uma pergunta que o motor clínico não responde: **quanto da
receita da clínica é atribuível ao produto, e não à inércia dos tutores.**

A resposta sai da comparação entre o grupo tratado e um grupo de controle de
~10% dos pets, sorteado de forma determinística por hash e efetivamente não
perseguido pelo motor. A diferença entre as duas taxas de cumprimento é o efeito
do produto; o resto teria acontecido de qualquer jeito.

O motor de protocolos vive no `clyvo-core` (Java 21 / Spring Boot sobre Oracle,
com PL/SQL como implementação das regras). **Este serviço não escreve no domínio
clínico.** Ele lê as views que o core publica e governa por migration apenas as
próprias tabelas, prefixadas `INS_`.

## Integrantes

| Nome | RM |
|---|---|
| Olavo Porto Neves | RM563558 |
| Pedro Henrique Dias França | RM561940 |
| Luiz Gustavo Gonçalves | RM564495 |
| Altamir Lima | RM562906 |
| Felipe Conte | RM562248 |

---

## Funcionalidades

- **Análise de coorte** — delta em pontos percentuais entre tratado e controle,
  consultas atribuíveis e receita recuperada, com os casos de borda tratados
  explicitamente (controle vazio, delta negativo, ticket ausente).
- **Resumo de obrigações** — distribuição por estado final, em baldes
  exclusivos, convertida do funil cumulativo que a view publica.
- **Metas de indicador** — CRUD completo sobre tabela própria, com as metas
  confrontadas com o valor apurado dentro da própria análise.
- **Projeções** — série diária de snapshots no Oracle e documento completo no
  MongoDB.
- **Observabilidade** — log estruturado em JSON com correlação por requisição,
  tracing e métricas do OpenTelemetry, health checks separando vida de
  prontidão, e métricas de desempenho em endpoint próprio.

---

## Arquitetura

```
Clyvo.Insights.sln
├─ src/
│  ├─ Clyvo.Insights.Domain          regra de coorte, pura, sem dependência
│  ├─ Clyvo.Insights.Application     casos de uso e portas de repositório
│  ├─ Clyvo.Insights.Infrastructure  EF Core/Oracle, MongoDB, health checks
│  └─ Clyvo.Insights.Api             controllers, JWT, Swagger, observabilidade
└─ tests/
   ├─ Clyvo.Insights.Tests.Unit         57 testes
   └─ Clyvo.Insights.Tests.Integration  26 testes
```

**A dependência aponta para dentro:**

```
        Api ──────┐
                  ├──► Application ──► Domain ──► (nada)
   Infrastructure ┘
```

`Domain` não referencia ninguém. `Application` referencia só `Domain`.
`Infrastructure` e `Api` referenciam `Application`. Nenhuma seta aponta para
fora — trocar Oracle por outro banco não toca em `Domain` nem em `Application`.

Sem MediatR, sem AutoMapper, sem FluentValidation. Caso de uso é classe com um
método público; mapeamento é método de extensão escrito à mão. A única
dependência de conveniência é o Moq, nos testes.

### Onde mora a regra

O cálculo é puro e vive no `Domain`:

- **`Coorte`** — grupo, obrigações resolvidas, obrigações cumpridas. A taxa é
  *derivada*, nunca recebida pronta.
- **`AnaliseCoorte`** — recebe as duas coortes e o ticket médio e produz delta em
  pontos percentuais, consultas atribuíveis e receita recuperada.
- **`ResumoObrigacoes`** — converte o funil cumulativo da view em baldes
  exclusivos por estado.
- **`MetaIndicador`** — o piso que a clínica define para um indicador.

**O denominador é só o que já foi resolvido.** Obrigação que ainda não venceu não
é falta: ela nem teve a chance. Contá-la afundaria as duas taxas e faria o número
andar sozinho com o calendário. A decisão está tomada do lado Oracle, na
`VW_CLV_PAINEL_COORTE`, e o Domínio a preserva como invariante.

---

## Contrato de integração com o core

| Objeto | Natureza | Quem é dono |
|---|---|---|
| `VW_CLV_PAINEL_COORTE` | view, leitura | core |
| `VW_CLV_PAINEL_RECEITA` | view, leitura | core |
| `INS_META_INDICADOR` | tabela, escrita | insights |
| `INS_SNAPSHOT_COORTE` | tabela, escrita | insights |
| `INS_EF_MIGRATIONS` | tabela, controle | insights |

### Por que as views não aparecem nas migrations

As views entram como entidades sem chave, mapeadas com
`HasNoKey().ToView("VW_CLV_PAINEL_COORTE")`, e o EF Core ignora entidades
mapeadas com `ToView` ao gerar migration. Isso é intencional: **migrations
governam só o que este serviço é dono, e o resto é leitura sobre contrato
publicado.** Se o insights emitisse DDL para as views, dois serviços passariam a
declarar o mesmo objeto e o último a rodar venceria.

O prefixo `INS_` não é decorativo: o schema Oracle da FIAP é compartilhado com as
tabelas `TB_CLV_` do core, e é ele que evita colisão. Pelo mesmo motivo a tabela
de histórico do EF foi renomeada de `__EFMigrationsHistory` para
`INS_EF_MIGRATIONS` — o nome padrão é de quem chegar primeiro.

### Onde encontrar a definição das views

`VW_CLV_PAINEL_COORTE` é criada pela migration **V11** do repositório do core, em
`Clyvo-Vet.java/server/src/main/resources/db/migration/V11__vw_clv_painel_coorte.sql`.

**Não está** no export de scripts em `sprint_03/database/` — aquele conjunto para
no V8 e só contém a `VW_CLV_PAINEL_RECEITA`. Quem aplicar apenas os scripts de lá
sobe um banco em que a análise de coorte não funciona.

### MongoDB

Guarda os snapshots apurados e o log de consulta de indicadores. A justificativa
é a forma, não a velocidade: o documento de snapshot muda conforme as metas que a
clínica tenha definido, e versionar isso em coluna relacional pediria migration a
cada indicador novo. **O Mongo não é cache do Oracle** — nada do que é gravado
nele volta a ser lido para responder requisição.

---

## Endpoints

| Método | Rota | Resposta |
|---|---|---|
| GET | `/api/insights/coorte` | 200 / 401 / 403 |
| GET | `/api/insights/obrigacoes/resumo` | 200 / 401 |
| GET | `/api/insights/metas` | 200 / 401 |
| POST | `/api/insights/metas` | 201 / 400 / 409 |
| PUT | `/api/insights/metas/{id}` | 200 / 400 / 404 |
| DELETE | `/api/insights/metas/{id}` | 204 / 404 |
| GET | `/health` | 200 — vivo |
| GET | `/health/ready` | 200 — pronto |
| GET | `/metrics` | 200 — desempenho |

**Nenhuma consulta recebe o id da clínica pelo cliente.** O tenant sai do claim
`idClinica` do token emitido pelo core, e os casos de uso não têm parâmetro de
clínica: não existe caminho para o id entrar por rota, query string ou corpo.

Swagger em `/swagger`, com esquema Bearer configurado.

### Autenticação

JWT compartilhado com o core: mesma chave simétrica, HS256 sobre os bytes UTF-8
do segredo. O insights **não emite** token, só valida o que o core emitiu. O core
não preenche issuer nem audience, então `ValidateIssuer` e `ValidateAudience`
ficam desligados de propósito — deixá-los no padrão recusaria todo token legítimo
com 401.

Obtenha um token no core:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"patricia@vidaanimal.com.br","senha":"Clyvo@2026","tipo":"COLABORADOR"}'
```

---

## Monitoramento

### Health checks

| Verificação | Aparece em | Falha como | Por quê |
|---|---|---|---|
| `self` | `/health` | — | Só diz que o processo responde |
| `oracle` | `/health/ready` | **Unhealthy** | Sem ele não há o que responder |
| `mongo` | `/health/ready` | Degraded | Só recebe projeção de saída |
| `clyvo-core` | `/health/ready` | Degraded | Não está no caminho de resposta |

**Vivo não é pronto.** `/health` não toca em banco nem em rede: se ele desse
unhealthy por Oracle fora, o orquestrador reiniciaria um processo saudável — e
reinício não traz banco de volta. `/health/ready` é que responde por dependência.

Só o Oracle derruba o serviço. O Mongo recebe apenas escrita de saída, e o
`clyvo-core` não é chamado para responder requisição nenhuma: o token é validado
localmente com a chave compartilhada, então com o core fora quem já tem token
válido continua sendo atendido. O que o degraded avisa é que nenhum token novo
está sendo emitido.

```bash
curl -s http://localhost:8081/health       | jq
curl -s http://localhost:8081/health/ready | jq
```

Ambos são anônimos: quem sonda saúde é o orquestrador, que não tem token do core.
A resposta traz cada verificação com status, descrição e duração; a exceção sai
só como mensagem, sem stack trace.

### Métricas de desempenho

```bash
curl -s http://localhost:8081/metrics | jq
```

```json
{
  "servico": "clyvo-insights",
  "requisicoes": 7,
  "taxaDeErro": 0.2857,
  "respostasPorFaixa": { "2xx": 5, "4xx": 2 },
  "duracaoPorRota": [
    { "rota": "GET api/insights/coorte", "requisicoes": 4,
      "duracaoMediaMs": 1089.34, "duracaoMaximaMs": 4041.69 }
  ]
}
```

O agrupamento é por **rota**, não por caminho: `/metas/7` e `/metas/9` são a mesma
rota. A medição não é refeita — o endpoint agrega, via `MeterListener`, o
histograma `http.server.request.duration` que o próprio ASP.NET Core publica.

### Logs

Serilog em JSON compacto, nos **dois destinos**:

| Destino | Onde | Para quê |
|---|---|---|
| Console | stdout | O que o orquestrador coleta (`docker logs api-clyvoinsights`) |
| Arquivo | `logs/clyvo-insights-<data>.log` | Sobrevive ao container ser recriado |

O arquivo fica relativo ao diretório de trabalho do processo — `/app/logs` no
container, `src/Clyvo.Insights.Api/logs/` rodando local. Rotação diária, sete
arquivos retidos. O caminho é configurável por `Serilog:Arquivo`.

**Níveis:** `Information` no ciclo de requisição, `Warning` em requisição
rejeitada e falha de projeção, `Error` em falha não tratada.

**Correlação.** Cada requisição carrega um `CorrelationId`, que é o mesmo
`TraceId` do OpenTelemetry — um único identificador liga log, span e resposta. Se
o chamador mandar o cabeçalho `X-Correlation-Id`, esse valor é respeitado; é o
caso do painel Java. A correlação sai no cabeçalho da resposta e no corpo do
`ProblemDetails`, então quem relata um erro tem o que informar.

Rastrear uma requisição de ponta a ponta:

```bash
# 1. o cliente recebeu o identificador no cabeçalho ou no corpo do erro
curl -sD - -o /dev/null http://localhost:8081/api/insights/coorte \
  -H "Authorization: Bearer $TOKEN" | grep -i x-correlation-id

# 2. todas as linhas daquela requisição
grep '"CorrelationId":"<valor>"' logs/clyvo-insights-*.log | jq
```

### Tracing

OpenTelemetry instrumenta ASP.NET Core, HttpClient e EF Core, mais métricas de
runtime, exportando para console. O health check é filtrado do tracing: ele bate
a cada poucos segundos e produziria mais span que requisição de verdade.

O exportador é o de console de propósito — coletor que ninguém sobe é
infraestrutura morta no compose. Trocar por OTLP é uma linha, no dia em que
houver para onde exportar.

---

## Executar

### Com Docker

O compose deste repositório sobe o Mongo e a API .NET, e entra na rede que o
compose do core já cria. Suba o core primeiro:

```bash
cd ../../Clyvo-Vet.java/server && docker compose up -d
cd -                            && docker compose up -d
```

API em `http://localhost:8081`, Swagger em `http://localhost:8081/swagger`.

### Local

Pré-requisitos: .NET 8 SDK (ou superior com o targeting pack do net8.0), Oracle
com o schema do core aplicado e MongoDB.

```bash
dotnet restore
dotnet run --project src/Clyvo.Insights.Api
```

O perfil `http` do `launchSettings.json` já aponta para os containers locais.
API em `http://localhost:5240`.

### Configuração

Segredo nenhum é versionado. O `appsettings.json` carrega a connection string do
Oracle FIAP com credencial de placeholder; o resto vem de variável de ambiente:

| Variável | Para quê |
|---|---|
| `ConnectionStrings__Oracle` | Oracle do core |
| `Jwt__Secret` | **a mesma** chave HMAC do core (`JWT_SECRET`) |
| `Mongo__ConnectionString` | MongoDB das projeções |
| `Core__BaseUrl` | clyvo-core, só para a sonda de disponibilidade |
| `Serilog__Arquivo` | caminho do log em arquivo (opcional) |

A aplicação **recusa a subir** sem `Jwt__Secret`, em vez de aceitar token não
assinado.

### Migrations

Migration é passo de deploy, não de inicialização: o container não altera schema
ao subir.

```bash
ConnectionStrings__Oracle="User Id=petflow;Password=PetFlow2026;Data Source=localhost:1521/PETFLOWDB" \
dotnet ef database update \
  -p src/Clyvo.Insights.Infrastructure \
  -s src/Clyvo.Insights.Infrastructure
```

---

## Testes

```bash
dotnet test
```

Roda a suíte inteira sem exigir banco nenhum: **57 unitários + 25 de integração
verdes, e 1 ignorado** — o que precisa de infraestrutura real (veja abaixo).

Um projeto por vez, ou um teste só:

```bash
dotnet test tests/Clyvo.Insights.Tests.Unit
dotnet test --filter "FullyQualifiedName~ObterCoorte_SemToken"
```

### Organização

Nomes seguem `MetodoTestado_Cenario_ResultadoEsperado`, e cada teste separa
Arrange, Act e Assert visualmente.

**Unitários** cobrem os casos de borda da regra — coorte de controle vazia, delta
negativo preservado com sinal, taxa de 100%, ticket médio zerado ou ausente,
degrau de funil fora de ordem. Os casos de uso da Aplicação usam **Moq** e
verificam orquestração e mapeamento; o Domínio é puro e não tem dublê.

**Integração** sobe a API em processo com `WebApplicationFactory` e exercita a
fatia HTTP: 401 sem token, 401 com token de outra chave, 403 sem o claim de
clínica, isolamento entre tenants e o CRUD de metas nos códigos que importam.

As fixtures dividem o host conforme quem escreve e quem só lê:

- `ColecaoDaApi` (`ICollectionFixture`) serve as classes que apenas leem — um
  host para todas, e a collection ainda serializa a execução delas;
- `MetasEndpointTests` usa `IClassFixture`, com host próprio, porque cria e apaga
  metas. O construtor limpa o estado antes de cada teste.

Os dados canônicos são fixados em fixture, e não lidos do seed: o seed do core
usa `SYS_GUID` e datas relativas a `SYSDATE`, então a contagem muda a cada
reexecução. O que se afirma são taxas, aritmética e forma da resposta.

### Testes contra infraestrutura real

Um grupo pequeno exercita o Oracle e o MongoDB de verdade — é a classe de defeito
que dublê não pega. Fica **ignorado por padrão**, no mesmo padrão do
`@EnabledIfEnvironmentVariable` do repositório Java: clone limpo sem banco tem de
ter a suíte verde, senão o vermelho deixa de significar "quebrou".

Para habilitar, com os containers de pé:

```bash
CLYVO_TESTES_DE_INFRA=1 \
ConnectionStrings__Oracle="User Id=petflow;Password=PetFlow2026;Data Source=localhost:1521/PETFLOWDB" \
Mongo__ConnectionString="mongodb://insights:Insights2026@localhost:27017/?authSource=admin" \
dotnet test tests/Clyvo.Insights.Tests.Integration
```

O principal deles compara, sobre cada linha da view, a taxa que o Domínio deriva
com o `PC_CUMPRIMENTO` que a view publica, com tolerância zero em duas casas.
Enquanto o painel Thymeleaf ler a coluna e esta API derivar, existem duas
implementações do mesmo número em produção — o teste transforma uma divergência
futura em falha de build, em vez de dois números diferentes na mesma
apresentação.

---

## Fora de escopo nesta entrega

HATEOAS, paginação, ordenação e filtros são da Sprint 4. Ingestão de eventos por
Service Bus e outbox também. O painel Thymeleaf continua lendo a view direto até
depois da pré-banca — a migração dele para consumir esta API é item pós-10/09, e
quando acontecer a consulta do lado Java sai do código, para não ficarem duas
implementações do mesmo número.
