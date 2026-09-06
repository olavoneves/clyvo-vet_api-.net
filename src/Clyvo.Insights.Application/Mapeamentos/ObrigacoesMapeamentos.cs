using Clyvo.Insights.Application.Obrigacoes;
using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Application.Mapeamentos;

public static class ObrigacoesMapeamentos
{
    public static ResumoObrigacoes ParaDominio(this LinhaFunilObrigacoes funil) =>
        ResumoObrigacoes.APartirDoFunil(
            funil.Total,
            funil.AlcancaramNotificada,
            funil.AlcancaramRespondida,
            funil.AlcancaramAgendada,
            funil.Cumpridas,
            funil.Perdidas);

    public static ResumoObrigacoesDto ParaDto(
        this ResumoObrigacoes resumo,
        long idClinica,
        LinhaFunilObrigacoes funil) =>
        new(idClinica,
            resumo.Total,
            resumo.Cumpridas,
            resumo.PararamEmAgendada,
            resumo.PararamEmRespondida,
            resumo.PararamEmNotificada,
            resumo.Perdidas,
            resumo.NaoTrabalhadas,
            funil.ValorRecuperado,
            funil.ValorPerdido,
            funil.MesInicio,
            funil.MesFim);
}
