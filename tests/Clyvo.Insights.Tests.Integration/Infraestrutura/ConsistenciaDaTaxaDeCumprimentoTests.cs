using Clyvo.Insights.Domain.Coortes;
using Microsoft.EntityFrameworkCore;

namespace Clyvo.Insights.Tests.Integration.Infraestrutura;

/// <summary>
/// Guarda contra as duas implementações do mesmo número divergirem.
/// </summary>
/// <remarks>
/// <para>
/// <c>PC_CUMPRIMENTO</c> existe na <c>VW_CLV_PAINEL_COORTE</c> e não é mapeada:
/// a taxa é derivada pelo domínio, a partir do numerador e do denominador, para
/// que o cálculo tenha um dono só. A decisão está certa e não muda aqui.
/// </para>
/// <para>
/// O problema é que hoje existem duas implementações em produção: o painel
/// Thymeleaf lê a coluna, esta API deriva. Enquanto as duas coexistirem — e vão
/// coexistir até a migração do painel, que é pós-banca —, uma divergência
/// aparece como dois números diferentes na mesma apresentação. Este teste a
/// transforma em falha de build.
/// </para>
/// <para>
/// Os candidatos a divergir são concretos, e nenhum deles é hipotético:
/// o Oracle arredonda meio-para-longe-do-zero e o padrão do .NET é
/// meio-para-o-par; o Oracle calcula <c>100 * cumpridas / total</c> em NUMBER de
/// 38 dígitos, enquanto o C# divide primeiro em decimal de 28. Uma mudança de
/// qualquer um dos lados quebra aqui, e não na tela.
/// </para>
/// </remarks>
[Collection(ColecaoDeInfraestruturaReal.Nome)]
public class ConsistenciaDaTaxaDeCumprimentoTests
{
    private readonly InfraestruturaRealFixture _infra;

    public ConsistenciaDaTaxaDeCumprimentoTests(InfraestruturaRealFixture infra)
    {
        _infra = infra;
    }

    [RequerInfraestrutura]
    public async Task TaxaCumprimento_SobreAsLinhasDaView_BateComPcCumprimentoDaColuna()
    {
        // Arrange — a coluna é lida por SQL direto de propósito. Mapeá-la na
        // entidade só para este teste abriria em produção o segundo caminho que
        // o desenho evita.
        await using var contexto = _infra.CriarContexto();
        var linhas = await LerLinhasBrutasAsync(contexto);

        Assert.NotEmpty(linhas);

        // Act & Assert
        foreach (var linha in linhas)
        {
            var coorte = Coorte.Criar(
                linha.Grupo == "CONTROLE" ? GrupoCoorte.Controle : GrupoCoorte.Tratado,
                linha.ObrigacoesResolvidas,
                linha.ObrigacoesCumpridas,
                petsDistintos: 0);

            var derivada = Math.Round(coorte.TaxaCumprimento * 100m, 2, MidpointRounding.AwayFromZero);

            Assert.True(
                derivada == linha.PcCumprimento,
                $"Clínica {linha.IdClinica}, grupo {linha.Grupo}: o domínio derivou {derivada} " +
                $"de {linha.ObrigacoesCumpridas}/{linha.ObrigacoesResolvidas}, e a view publicou " +
                $"{linha.PcCumprimento} em PC_CUMPRIMENTO. Os dois números aparecem lado a lado no " +
                "painel, então um deles está errado na tela agora.");
        }
    }

    private static async Task<List<LinhaBruta>> LerLinhasBrutasAsync(DbContext contexto)
    {
        const string sql = """
            SELECT id_clinica, ds_grupo, qt_obrigacoes, qt_cumpridas, pc_cumprimento
              FROM VW_CLV_PAINEL_COORTE
             ORDER BY id_clinica, ds_grupo
            """;

        var conexao = contexto.Database.GetDbConnection();
        await contexto.Database.OpenConnectionAsync();

        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;

        var linhas = new List<LinhaBruta>();

        await using var leitor = await comando.ExecuteReaderAsync();

        while (await leitor.ReadAsync())
        {
            linhas.Add(new LinhaBruta(
                leitor.GetInt64(0),
                leitor.GetString(1),
                leitor.GetInt32(2),
                leitor.GetInt32(3),
                // NULLIF na view: clínica sem obrigação resolvida some do
                // agrupamento, mas a coluna admite nulo e o leitor precisa saber.
                leitor.IsDBNull(4) ? 0m : leitor.GetDecimal(4)));
        }

        return linhas;
    }

    private sealed record LinhaBruta(
        long IdClinica,
        string Grupo,
        int ObrigacoesResolvidas,
        int ObrigacoesCumpridas,
        decimal PcCumprimento);
}
