# Clyvo Insights — serviço .NET de leitura e análise

Serviço ASP.NET Core 8 do ecossistema **Clyvo Vet**. Ele é o lado de *leitura*:
responde quanto da receita da clínica é atribuível ao produto, e não à inércia
dos tutores, comparando o grupo tratado com um grupo de controle de ~10% dos
pets sorteado deterministicamente pelo motor.

O motor de protocolos clínicos vive no `clyvo-core` (Java 21 / Spring Boot sobre
Oracle, com PL/SQL como implementação das regras). **Este serviço não escreve no
domínio clínico** — ele lê as views que o core publica e governa por migration
apenas as próprias tabelas, prefixadas `INS_`.

## Arquitetura

```
Clyvo.Insights.sln
├─ src/
│  ├─ Clyvo.Insights.Domain          regra de coorte, pura, sem dependência
│  ├─ Clyvo.Insights.Application     casos de uso e portas de repositório
│  ├─ Clyvo.Insights.Infrastructure  EF Core/Oracle e MongoDB
│  └─ Clyvo.Insights.Api             controllers, JWT, Swagger, observabilidade
└─ tests/
   ├─ Clyvo.Insights.Tests.Unit
   └─ Clyvo.Insights.Tests.Integration
```

A dependência aponta para dentro: `Domain` não referencia ninguém, `Application`
referencia só `Domain`, `Infrastructure` e `Api` referenciam `Application`.

## Contrato de integração com o core

| Objeto | Natureza | Quem é dono |
|---|---|---|
| `VW_CLV_PAINEL_COORTE` | view, leitura | core |
| `VW_CLV_PAINEL_RECEITA` | view, leitura | core |
| `INS_META_INDICADOR` | tabela, escrita | insights |
| `INS_SNAPSHOT_COORTE` | tabela, escrita | insights |

Migrations EF governam só o que este serviço é dono. As views entram como
entidades sem chave (`HasNoKey().ToView(...)`), que o EF ignora ao gerar
migration.

## Executar

Pré-requisitos: .NET 8 SDK (ou superior com targeting pack do net8.0) e uma
instância Oracle com o schema do core aplicado.

```bash
dotnet restore
dotnet run --project src/Clyvo.Insights.Api
```

O perfil `http` do `launchSettings.json` já aponta para o container Oracle local
(`localhost:1521/PETFLOWDB`). Para outro banco, sobrescreva a connection string
por variável de ambiente:

```bash
ConnectionStrings__Oracle="User Id=RM000000;Password=...;Data Source=oracle.fiap.com.br:1521/orcl"
```

Swagger em `http://localhost:5240/swagger`.

## Testes

```bash
dotnet test
```
