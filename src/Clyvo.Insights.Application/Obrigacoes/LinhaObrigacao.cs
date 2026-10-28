using Clyvo.Insights.Domain.Coortes;
using Clyvo.Insights.Domain.Obrigacoes;

namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>
/// Uma linha de <c>VW_CLV_OBRIGACAO_DETALHE</c> como o repositório a devolve.
/// </summary>
/// <remarks>
/// Modelo de leitura, como <see cref="Coortes.LinhaCoorte"/>: o status e o
/// grupo já chegam traduzidos para o vocabulário do domínio, e o texto cru da
/// view não passa daqui.
/// </remarks>
/// <param name="IdObrigacao"><c>ID_OBRIGACAO</c>.</param>
/// <param name="IdPet"><c>ID_PET</c>.</param>
/// <param name="NomePet"><c>NM_PET</c>.</param>
/// <param name="NomeTutor"><c>NM_TUTOR</c>. Só o nome: a view não publica contato.</param>
/// <param name="Protocolo"><c>NM_PROTOCOLO</c>.</param>
/// <param name="Categoria"><c>DS_CATEGORIA</c> do protocolo.</param>
/// <param name="Etapa"><c>NM_ETAPA</c>.</param>
/// <param name="Status"><c>DS_STATUS</c>.</param>
/// <param name="Grupo"><c>FL_GRUPO_CONTROLE</c>: 'S' controle, 'N' tratado.</param>
/// <param name="DataPrevista"><c>DT_PREVISTA</c>.</param>
/// <param name="JanelaInicio"><c>DT_JANELA_INICIO</c>.</param>
/// <param name="JanelaFim"><c>DT_JANELA_FIM</c>.</param>
/// <param name="ValorEstimado"><c>NR_VALOR_ESTIMADO</c>, nulo quando o protocolo não tem preço.</param>
/// <param name="ValorRealizado"><c>NR_VALOR_REALIZADO</c>, nulo até a obrigação ser cumprida.</param>
public sealed record LinhaObrigacao(
    long IdObrigacao,
    long IdPet,
    string NomePet,
    string NomeTutor,
    string Protocolo,
    string Categoria,
    string Etapa,
    StatusObrigacao Status,
    GrupoCoorte Grupo,
    DateTime DataPrevista,
    DateTime JanelaInicio,
    DateTime JanelaFim,
    decimal? ValorEstimado,
    decimal? ValorRealizado);
