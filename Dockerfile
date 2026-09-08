# ============================================================
#  CLYVO INSIGHTS — Dockerfile
#  Multi-stage: SDK (build) + ASP.NET runtime (execucao)
# ============================================================

# --- Estagio 1: restore e publish ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /src

# Os csproj entram sozinhos primeiro para que o restore fique numa camada que
# so e invalidada quando uma dependencia muda — e nao a cada linha de codigo.
COPY Clyvo.Insights.sln .
COPY src/Clyvo.Insights.Domain/Clyvo.Insights.Domain.csproj                 src/Clyvo.Insights.Domain/
COPY src/Clyvo.Insights.Application/Clyvo.Insights.Application.csproj       src/Clyvo.Insights.Application/
COPY src/Clyvo.Insights.Infrastructure/Clyvo.Insights.Infrastructure.csproj src/Clyvo.Insights.Infrastructure/
COPY src/Clyvo.Insights.Api/Clyvo.Insights.Api.csproj                       src/Clyvo.Insights.Api/
COPY tests/Clyvo.Insights.Tests.Unit/Clyvo.Insights.Tests.Unit.csproj               tests/Clyvo.Insights.Tests.Unit/
COPY tests/Clyvo.Insights.Tests.Integration/Clyvo.Insights.Tests.Integration.csproj tests/Clyvo.Insights.Tests.Integration/
RUN dotnet restore Clyvo.Insights.sln

COPY src/ src/
COPY tests/ tests/
RUN dotnet publish src/Clyvo.Insights.Api/Clyvo.Insights.Api.csproj \
        -c Release -o /app/publish --no-restore


# --- Estagio 2: imagem de execucao ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# curl entra so para o healthcheck do compose: a imagem de runtime nao traz
# nenhum cliente HTTP, e sem ele o compose nao consegue sondar /health.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system appuser && useradd --system --gid appuser appuser

WORKDIR /app
COPY --from=builder /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081

ENTRYPOINT ["dotnet", "Clyvo.Insights.Api.dll"]
