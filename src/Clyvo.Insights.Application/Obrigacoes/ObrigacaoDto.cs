using Clyvo.Insights.Application.Paginacao;

namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>Uma obrigação clínica da clínica do token, como sai na resposta.</summary>
/// <param name="Id">Identificador da obrigação no core.</param>
/// <param name="IdPet">Pet a que a obrigação se refere.</param>
/// <param name="NomePet">Nome do pet.</param>
/// <param name="NomeTutor">Nome do tutor. Contato não sai do core.</param>
/// <param name="Protocolo">Protocolo clínico que gerou a obrigação.</param>
/// <param name="Categoria">Categoria do protocolo (VACINA, VERMIFUGO, EXAME...).</param>
/// <param name="Etapa">Etapa do protocolo.</param>
/// <param name="Status">PREVISTA, NOTIFICADA, RESPONDIDA, AGENDADA, CUMPRIDA, PERDIDA ou CANCELADA.</param>
/// <param name="Grupo">TRATADO ou CONTROLE — o mesmo rótulo da análise de coorte.</param>
/// <param name="DataPrevista">Quando a obrigação vence.</param>
/// <param name="JanelaInicio">Início da janela clínica aceitável.</param>
/// <param name="JanelaFim">Fim da janela clínica aceitável.</param>
/// <param name="ValorEstimado">Valor estimado do atendimento, se o protocolo tiver preço.</param>
/// <param name="ValorRealizado">Valor efetivamente cobrado, depois de cumprida.</param>
public sealed record ObrigacaoDto(
    long Id,
    long IdPet,
    string NomePet,
    string NomeTutor,
    string Protocolo,
    string Categoria,
    string Etapa,
    string Status,
    string Grupo,
    DateOnly DataPrevista,
    DateOnly JanelaInicio,
    DateOnly JanelaFim,
    decimal? ValorEstimado,
    decimal? ValorRealizado);

/// <summary>
/// Uma página da listagem, junto com os critérios que a produziram.
/// </summary>
/// <remarks>
/// Os critérios voltam com a página porque é a partir deles que os links de
/// navegação são montados: <c>next</c> precisa repetir o mesmo filtro e a mesma
/// ordenação, só mudando o número da página.
/// </remarks>
/// <param name="Pagina">Os itens e a aritmética de paginação.</param>
/// <param name="Criterios">A consulta já validada.</param>
public sealed record ListagemObrigacoesDto(
    Pagina<ObrigacaoDto> Pagina,
    CriteriosListagemObrigacoes Criterios);
