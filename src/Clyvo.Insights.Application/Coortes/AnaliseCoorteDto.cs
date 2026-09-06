using Clyvo.Insights.Application.Metas;

namespace Clyvo.Insights.Application.Coortes;

/// <summary>
/// Um braço do experimento como sai na resposta.
/// </summary>
/// <param name="Grupo">TRATADO ou CONTROLE.</param>
/// <param name="ObrigacoesResolvidas">Obrigações que chegaram ao fim — o denominador.</param>
/// <param name="ObrigacoesCumpridas">Dessas, quantas o tutor cumpriu.</param>
/// <param name="TaxaCumprimentoPercentual">Percentual de cumprimento, de 0 a 100.</param>
/// <param name="PetsDistintos">Tamanho da coorte em pets.</param>
public sealed record CoorteDto(
    string Grupo,
    int ObrigacoesResolvidas,
    int ObrigacoesCumpridas,
    decimal TaxaCumprimentoPercentual,
    int PetsDistintos);

/// <summary>
/// Quanto da receita da clínica é atribuível ao produto, e não à inércia dos
/// tutores.
/// </summary>
/// <param name="IdClinica">Clínica do token. Nunca vem por parâmetro da requisição.</param>
/// <param name="Disponivel">
/// Falso quando falta denominador em algum dos braços. Os números vêm zerados e
/// não devem ser exibidos como resultado.
/// </param>
/// <param name="MotivoIndisponibilidade">Por que não deu para calcular. Nulo quando disponível.</param>
/// <param name="DeltaPontosPercentuais">
/// Diferença entre as taxas de cumprimento. Pode ser negativo — significa que o
/// grupo perseguido compareceu menos que o deixado de fora.
/// </param>
/// <param name="ConsultasAtribuiveis">Obrigações resolvidas do tratado multiplicadas pelo delta.</param>
/// <param name="ReceitaRecuperada">Consultas atribuíveis ao ticket médio.</param>
/// <param name="ReceitaEstimavel">
/// Falso quando a receita saiu zerada por falta de ticket médio, e não por
/// falta de efeito.
/// </param>
/// <param name="TicketMedio">Ticket médio apurado das consultas realizadas da clínica.</param>
/// <param name="Tratado">Coorte dos pets perseguidos pelo motor.</param>
/// <param name="Controle">Coorte de controle, o contrafactual.</param>
/// <param name="Metas">
/// Metas da clínica confrontadas com o que a análise apurou. Vem vazia quando a
/// análise está indisponível — sinalizar meta não atingida contra números
/// zerados por falta de contrafactual seria alarme falso.
/// </param>
public sealed record AnaliseCoorteDto(
    long IdClinica,
    bool Disponivel,
    string? MotivoIndisponibilidade,
    decimal DeltaPontosPercentuais,
    decimal ConsultasAtribuiveis,
    decimal ReceitaRecuperada,
    bool ReceitaEstimavel,
    decimal? TicketMedio,
    CoorteDto Tratado,
    CoorteDto Controle,
    IReadOnlyList<MetaAvaliadaDto> Metas);
