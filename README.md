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
   └─ Clyvo.Insights.Tests.Integration  24 testes
```

A dependência aponta para dentro: `Domain` não referencia ninguém, `Application`
referencia só `Domain`, `Infrastructure` e `Api` referenciam `Application`.

Sem MediatR, sem AutoMapper, sem FluentValidation. Caso de uso é classe com um
método público; mapeamento é método de extensão escrito à mão. A única
dependência de conveniência é o Moq, nos testes.

### Onde mora a regra

O cálculo de coorte é puro e vive no `Domain`:

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

As views entram como entidades sem chave (`HasNoKey().ToView(...)`), e o EF as
ignora ao gerar migration — **migrations governam só o que este serviço é dono, o
resto é leitura sobre contrato publicado.** O prefixo `INS_` não é decorativo: o
schema Oracle da FIAP é compartilhado com as tabelas `TB_CLV_` do core, e é ele
que evita colisão.

O MongoDB guarda os snapshots apurados e o log de consulta de indicadores. A
justificativa é a forma, não a velocidade: o documento de snapshot muda conforme
as metas que a clínica tenha definido, e versionar isso em coluna relacional
pediria migration a cada indicador novo. **O Mongo não é cache do Oracle** —
nada do que é gravado nele volta a ser lido para responder requisição.

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
| GET | `/health` | vivo — não toca em banco |
| GET | `/health/ready` | pronto — Oracle e Mongo |

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

**Unitários** cobrem os casos de borda da regra: coorte de controle vazia, delta
negativo preservado com sinal, taxa de 100%, ticket médio zerado ou ausente,
degrau de funil fora de ordem.

**Integração** sobe a API em processo com `WebApplicationFactory` e exercita a
fatia HTTP: 401 sem token, 401 com token de outra chave, 403 sem o claim de
clínica, isolamento entre tenants e o CRUD de metas nos códigos que importam.

Os dados canônicos são fixados em fixture, e não lidos do seed: o seed do core
usa `SYS_GUID` e datas relativas a `SYSDATE`, então a contagem muda a cada
reexecução. O que se afirma são taxas, aritmética e forma da resposta.

---

## Fora de escopo nesta entrega

HATEOAS, paginação, ordenação e filtros são da Sprint 4. Ingestão de eventos por
Service Bus e outbox também. O painel Thymeleaf continua lendo a view direto até
depois da pré-banca — a migração dele para consumir esta API é item pós-10/09, e
quando acontecer a consulta do lado Java sai do código, para não ficarem duas
implementações do mesmo número.
