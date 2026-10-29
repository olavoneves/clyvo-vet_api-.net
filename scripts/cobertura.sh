#!/usr/bin/env sh
# ============================================================
#  CLYVO INSIGHTS — relatório de cobertura
#
#  Mesmo fluxo do cobertura.ps1: suíte com coverlet.collector, junção dos
#  arquivos Cobertura com o reportgenerator do manifesto local e o resumo
#  publicado em docs/cobertura/<modo>/Summary.md. O HTML fica em TestResults/.
#  Nada é excluído da medição.
#
#  Sem CLYVO_TESTES_DE_INFRA, o modo é sem-infraestrutura. Com
#  CLYVO_TESTES_DE_INFRA=1, é com-infraestrutura: o script imprime o host do
#  Oracle e aborta se ele for da FIAP.
#
#  Uso, da raiz do repositório:
#      ./scripts/cobertura.sh
#      CLYVO_TESTES_DE_INFRA=1 ./scripts/cobertura.sh
# ============================================================
set -eu

if [ "${CLYVO_TESTES_DE_INFRA:-}" = "1" ]; then
    modo="com-infraestrutura"

    if [ -z "${ConnectionStrings__Oracle:-}" ]; then
        echo "CLYVO_TESTES_DE_INFRA=1 sem ConnectionStrings__Oracle definida." >&2
        exit 1
    fi

    host_oracle=$(printf '%s' "$ConnectionStrings__Oracle" | sed -n 's/.*Data Source=\([^;]*\).*/\1/p')
    echo "Oracle — host resolvido: $host_oracle"

    case "$host_oracle" in
        *fiap.com.br*)
            echo "ABORTADO: a connection string aponta para o Oracle da FIAP. Use um banco local." >&2
            exit 1 ;;
    esac
else
    modo="sem-infraestrutura"
fi

# Caminhos relativos à raiz: no Git Bash do Windows, um absoluto /c/... chega
# ao reportgenerator sem conversão, dentro do argumento -reports:, e não casa.
cd "$(dirname "$0")/.."
resultados="TestResults/cobertura/$modo"
relatorio="$resultados/relatorio"
destino="docs/cobertura/$modo"

rm -rf "$resultados"

dotnet tool restore
dotnet test Clyvo.Insights.sln --collect:"XPlat Code Coverage" --results-directory "$resultados"

dotnet reportgenerator \
    "-reports:$resultados/**/coverage.cobertura.xml" \
    "-targetdir:$relatorio" \
    "-reporttypes:MarkdownSummary;Html" \
    "-title:Clyvo Insights ($modo)"

mkdir -p "$destino"
cp "$relatorio/Summary.md" "$destino/Summary.md"

echo "Resumo em $destino/Summary.md; HTML em $relatorio/index.html"
