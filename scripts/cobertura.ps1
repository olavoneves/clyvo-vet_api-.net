# ============================================================
#  CLYVO INSIGHTS — relatório de cobertura
#
#  Roda a suíte com o coverlet.collector que já está nos csproj, junta os
#  dois arquivos Cobertura (unitário e integração) com o reportgenerator do
#  manifesto local (.config/dotnet-tools.json) e publica o resumo em
#  docs/cobertura/Summary.md. O HTML completo fica em TestResults/, fora do Git.
#
#  Sem CLYVO_TESTES_DE_INFRA, os testes de Oracle e Mongo são ignorados e a
#  medição é a da suíte que qualquer clone roda.
#
#  Uso, da raiz do repositório:
#      ./scripts/cobertura.ps1
# ============================================================

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$resultados = Join-Path $raiz 'TestResults/cobertura'
$relatorio = Join-Path $resultados 'relatorio'
$destino = Join-Path $raiz 'docs/cobertura'

Push-Location $raiz
try {
    if (Test-Path $resultados) {
        Remove-Item -Recurse -Force $resultados
    }

    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore falhou.' }

    dotnet test Clyvo.Insights.sln --collect:"XPlat Code Coverage" --results-directory $resultados
    if ($LASTEXITCODE -ne 0) { throw 'A suíte falhou; sem relatório de cobertura.' }

    dotnet reportgenerator `
        "-reports:$resultados/**/coverage.cobertura.xml" `
        "-targetdir:$relatorio" `
        "-reporttypes:MarkdownSummary;Html" `
        "-title:Clyvo Insights"
    if ($LASTEXITCODE -ne 0) { throw 'reportgenerator falhou.' }

    New-Item -ItemType Directory -Force $destino | Out-Null
    Copy-Item (Join-Path $relatorio 'Summary.md') (Join-Path $destino 'Summary.md') -Force

    Write-Host "Resumo em docs/cobertura/Summary.md; HTML em $relatorio/index.html"
}
finally {
    Pop-Location
}
