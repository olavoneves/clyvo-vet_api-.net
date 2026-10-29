#!/usr/bin/env sh
# ============================================================
#  CLYVO INSIGHTS — relatório de cobertura
#
#  Mesmo fluxo do cobertura.ps1: suíte com coverlet.collector, junção dos
#  arquivos Cobertura com o reportgenerator do manifesto local e o resumo
#  publicado em docs/cobertura/Summary.md. O HTML fica em TestResults/.
#
#  Uso, da raiz do repositório:
#      ./scripts/cobertura.sh
# ============================================================
set -eu

raiz="$(cd "$(dirname "$0")/.." && pwd)"
resultados="$raiz/TestResults/cobertura"
relatorio="$resultados/relatorio"
destino="$raiz/docs/cobertura"

cd "$raiz"
rm -rf "$resultados"

dotnet tool restore
dotnet test Clyvo.Insights.sln --collect:"XPlat Code Coverage" --results-directory "$resultados"

dotnet reportgenerator \
    "-reports:$resultados/**/coverage.cobertura.xml" \
    "-targetdir:$relatorio" \
    "-reporttypes:MarkdownSummary;Html" \
    "-title:Clyvo Insights"

mkdir -p "$destino"
cp "$relatorio/Summary.md" "$destino/Summary.md"

echo "Resumo em docs/cobertura/Summary.md; HTML em $relatorio/index.html"
