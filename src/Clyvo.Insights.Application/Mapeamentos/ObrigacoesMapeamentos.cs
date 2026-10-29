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

    /// <summary>
    /// Projeta a linha da view na resposta. As datas viram <c>DateOnly</c>: o
    /// core grava DATE sem hora significativa, e devolver meia-noite com fuso
    /// sugeriria uma precisão que o dado não tem.
    /// </summary>
    public static ObrigacaoDto ParaDto(this LinhaObrigacao linha) =>
        new(linha.IdObrigacao,
            linha.IdPet,
            linha.NomePet,
            linha.NomeTutor,
            linha.Protocolo,
            linha.Categoria,
            linha.Etapa,
            linha.Status.Codigo(),
            linha.Grupo.ToString().ToUpperInvariant(),
            DateOnly.FromDateTime(linha.DataPrevista),
            DateOnly.FromDateTime(linha.JanelaInicio),
            DateOnly.FromDateTime(linha.JanelaFim),
            linha.ValorEstimado,
            linha.ValorRealizado);
}
