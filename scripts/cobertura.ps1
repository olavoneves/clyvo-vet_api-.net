# ============================================================
#  CLYVO INSIGHTS — relatório de cobertura
#
#  Roda a suíte com o coverlet.collector que já está nos csproj, junta os
#  dois arquivos Cobertura (unitário e integração) com o reportgenerator do
#  manifesto local (.config/dotnet-tools.json) e publica o resumo em
#  docs/cobertura/<modo>/Summary.md. O HTML completo fica em TestResults/,
#  fora do Git. Nada é excluído da medição.
#
#  Dois modos, publicados lado a lado:
#    - sem-infraestrutura: a suíte que qualquer clone roda; os testes de
#      Oracle e Mongo são ignorados.
#    - com-infraestrutura: CLYVO_TESTES_DE_INFRA=1, com ConnectionStrings__Oracle
#      e Mongo__ConnectionString apontando para bancos locais. Antes de rodar,
#      o script imprime o host do Oracle e aborta se ele for da FIAP.
#
#  Uso, da raiz do repositório:
#      ./scripts/cobertura.ps1
#      $env:CLYVO_TESTES_DE_INFRA = '1'; ./scripts/cobertura.ps1
# ============================================================

$ErrorActionPreference = 'Stop'

$comInfra = $env:CLYVO_TESTES_DE_INFRA -eq '1'
$modo = if ($comInfra) { 'com-infraestrutura' } else { 'sem-infraestrutura' }

if ($comInfra) {
    $conexao = $env:ConnectionStrings__Oracle
    if ([string]::IsNullOrWhiteSpace($conexao)) {
        throw 'CLYVO_TESTES_DE_INFRA=1 sem ConnectionStrings__Oracle definida.'
    }

    $hostOracle = if ($conexao -match 'Data Source=([^;]*)') { $Matches[1] } else { '' }
    Write-Host "Oracle — host resolvido: $hostOracle"

    if ($hostOracle -like '*fiap.com.br*') {
        throw 'ABORTADO: a connection string aponta para o Oracle da FIAP. Use um banco local.'
    }
}

$raiz = Split-Path -Parent $PSScriptRoot
$resultados = Join-Path $raiz "TestResults/cobertura/$modo"
$relatorio = Join-Path $resultados 'relatorio'
$destino = Join-Path $raiz "docs/cobertura/$modo"

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
        "-title:Clyvo Insights ($modo)"
    if ($LASTEXITCODE -ne 0) { throw 'reportgenerator falhou.' }

    New-Item -ItemType Directory -Force $destino | Out-Null
    Copy-Item (Join-Path $relatorio 'Summary.md') (Join-Path $destino 'Summary.md') -Force

    Write-Host "Resumo em docs/cobertura/$modo/Summary.md; HTML em $relatorio/index.html"
}
finally {
    Pop-Location
}
