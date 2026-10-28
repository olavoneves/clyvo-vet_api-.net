using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Tests.Integration.Fixtures;

/// <summary>
/// Dados canônicos das clínicas de teste.
/// </summary>
/// <remarks>
/// <para>
/// As formas vêm do banco de demonstração, mas os números são fixados aqui de
/// propósito. O seed do core usa <c>SYS_GUID</c> e datas relativas a
/// <c>SYSDATE</c>: a contagem muda a cada reexecução, e um teste que fixasse
/// "2715 obrigações" passaria hoje e quebraria amanhã sem nada ter mudado no
/// código.
/// </para>
/// <para>
/// Com a fixture no controle, o que se afirma sobre a resposta são taxas,
/// aritmética e forma — que são justamente as coisas que o serviço promete e
/// que uma mudança de seed não pode alterar.
/// </para>
/// </remarks>
public static class DadosCanonicos
{
    /// <summary>Clínica com as duas coortes povoadas.</summary>
    public const long ClinicaComCoorte = 23;

    /// <summary>Clínica vizinha. Serve para provar que um tenant não vê o outro.</summary>
    public const long ClinicaVizinha = 24;

    /// <summary>Clínica sem obrigação resolvida: a análise sai indisponível.</summary>
    public const long ClinicaSemDados = 99;

    public const decimal TicketDaClinicaComCoorte = 200m;
    public const decimal TicketDaVizinha = 100m;

    public static IReadOnlyList<LinhaCoorte> CoortesDaClinicaComCoorte { get; } = new[]
    {
        // 60% contra 40%: delta de 20 p.p. redondos, para a aritmética da
        // resposta poder ser conferida de cabeça na leitura do teste.
        new LinhaCoorte(GrupoCoorte.Tratado, 1000, 600, 120, TicketDaClinicaComCoorte),
        new LinhaCoorte(GrupoCoorte.Controle, 100, 40, 12, TicketDaClinicaComCoorte)
    };

    public static IReadOnlyList<LinhaCoorte> CoortesDaVizinha { get; } = new[]
    {
        // 50% contra 25%: delta de 25 p.p., diferente do da outra clínica, para
        // que trocar um tenant pelo outro apareça no resultado.
        new LinhaCoorte(GrupoCoorte.Tratado, 800, 400, 90, TicketDaVizinha),
        new LinhaCoorte(GrupoCoorte.Controle, 80, 20, 9, TicketDaVizinha)
    };

    public static LinhaFunilObrigacoes FunilDaClinicaComCoorte { get; } = new(
        Total: 100,
        AlcancaramNotificada: 80,
        AlcancaramRespondida: 60,
        AlcancaramAgendada: 45,
        Cumpridas: 30,
        Perdidas: 15,
        ValorRecuperado: 6000m,
        ValorPerdido: 3000m,
        MesInicio: new DateTime(2025, 3, 1),
        MesFim: new DateTime(2026, 9, 1));

    public static LinhaFunilObrigacoes FunilVazio { get; } =
        new(0, 0, 0, 0, 0, 0, 0m, 0m, null, null);

    // Declarado antes de Obrigacoes: inicializador estático roda na ordem do
    // arquivo, e a lista abaixo usa este array.
    private static readonly string[] Categorias = { "VACINA", "VERMIFUGO", "EXAME" };

    /// <summary>
    /// Obrigações da clínica com coorte: 45, para que a página padrão de 20
    /// produza três páginas — primeira, meio e última — com a última incompleta.
    /// </summary>
    public const int TotalDeObrigacoesDaClinica = 45;

    /// <summary>Uma obrigação que existe, mas na clínica vizinha.</summary>
    public const long ObrigacaoDaVizinha = 9001;

    /// <summary>
    /// Obrigações por clínica, geradas de forma determinística.
    /// </summary>
    /// <remarks>
    /// O status percorre os sete estados em ordem, então a obrigação <c>i</c>
    /// tem o status <c>(i - 1) % 7 + 1</c>; uma em cada cinco é de controle; a
    /// data prevista avança um dia por obrigação a partir de 01/01/2026. Com
    /// isso, cada filtro tem uma contagem que dá para conferir de cabeça.
    /// </remarks>
    public static IReadOnlyDictionary<long, IReadOnlyList<LinhaObrigacao>> Obrigacoes { get; } =
        new Dictionary<long, IReadOnlyList<LinhaObrigacao>>
        {
            [ClinicaComCoorte] = Enumerable.Range(1, TotalDeObrigacoesDaClinica).Select(i => Obrigacao(i)).ToList(),
            [ClinicaVizinha] = new[] { Obrigacao(1, ObrigacaoDaVizinha), Obrigacao(2, ObrigacaoDaVizinha + 1) }
        };

    private static LinhaObrigacao Obrigacao(int i, long? id = null)
    {
        var status = (StatusObrigacao)((i - 1) % 7 + 1);
        var prevista = new DateTime(2026, 1, 1).AddDays(i - 1);

        return new LinhaObrigacao(
            IdObrigacao: id ?? i,
            IdPet: 100 + i % 5,
            NomePet: $"Pet {(char)('A' + i % 5)}",
            NomeTutor: $"Tutor {i % 5}",
            Protocolo: "Protocolo de teste",
            Categoria: Categorias[i % Categorias.Length],
            Etapa: $"Etapa {i % 3 + 1}",
            Status: status,
            Grupo: i % 5 == 0 ? GrupoCoorte.Controle : GrupoCoorte.Tratado,
            DataPrevista: prevista,
            JanelaInicio: prevista.AddDays(-7),
            JanelaFim: prevista.AddDays(7),
            ValorEstimado: 100m + i,
            ValorRealizado: status == StatusObrigacao.Cumprida ? 100m + i : null);
    }
}
