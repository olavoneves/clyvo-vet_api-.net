using Clyvo.Insights.Application.Coortes;
using Clyvo.Insights.Application.Metas;
using Clyvo.Insights.Domain.Coortes;

namespace Clyvo.Insights.Application.Mapeamentos;

/// <summary>
/// Mapeamento entre o modelo de leitura, o domínio e o DTO de resposta.
/// Escrito à mão de propósito: são três projeções curtas, e uma biblioteca de
/// mapeamento aqui só acrescentaria uma dependência e uma configuração para
/// explicar.
/// </summary>
public static class CoorteMapeamentos
{
    /// <summary>
    /// Constrói a coorte do grupo pedido. Grupo ausente na leitura vira coorte
    /// vazia — é o caso da clínica cujo controle ainda não teve obrigação
    /// resolvida, e o domínio sabe tratá-lo.
    /// </summary>
    public static Coorte ParaCoorteDoGrupo(this IReadOnlyList<LinhaCoorte> linhas, GrupoCoorte grupo)
    {
        var linha = linhas.FirstOrDefault(l => l.Grupo == grupo);

        return linha is null
            ? Coorte.Criar(grupo, obrigacoesResolvidas: 0, obrigacoesCumpridas: 0, petsDistintos: 0)
            : Coorte.Criar(grupo, linha.ObrigacoesResolvidas, linha.ObrigacoesCumpridas, linha.PetsDistintos);
    }

    /// <summary>
    /// Ticket médio da clínica. A view repete o valor nas duas linhas porque
    /// ele é atributo da clínica e não do grupo, então basta a primeira que o
    /// tiver preenchido.
    /// </summary>
    public static decimal? ParaTicketMedio(this IReadOnlyList<LinhaCoorte> linhas) =>
        linhas.Select(l => l.TicketMedio).FirstOrDefault(t => t is not null);

    /// <summary>Projeta a análise do domínio na resposta da API.</summary>
    public static AnaliseCoorteDto ParaDto(this AnaliseCoorte analise, long idClinica) =>
        new(idClinica,
            analise.Disponivel,
            analise.MotivoIndisponibilidade,
            analise.DeltaPontosPercentuais,
            analise.ConsultasAtribuiveis,
            analise.ReceitaRecuperada,
            analise.ReceitaEstimavel,
            analise.TicketMedio,
            analise.Tratado.ParaDto(),
            analise.Controle.ParaDto(),
            Array.Empty<MetaAvaliadaDto>());

    private static CoorteDto ParaDto(this Coorte coorte) =>
        new(coorte.Grupo.ToString().ToUpperInvariant(),
            coorte.ObrigacoesResolvidas,
            coorte.ObrigacoesCumpridas,
            Math.Round(coorte.TaxaCumprimento * 100m, 2, MidpointRounding.AwayFromZero),
            coorte.PetsDistintos);
}
