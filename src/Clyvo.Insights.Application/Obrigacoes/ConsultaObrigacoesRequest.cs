namespace Clyvo.Insights.Application.Obrigacoes;

/// <summary>
/// Parâmetros de consulta da listagem de obrigações, como chegam da query
/// string. Todos opcionais e combináveis.
/// </summary>
/// <remarks>
/// Texto cru, de propósito: a validação e a interpretação acontecem num lugar
/// só, em <see cref="CriteriosListagemObrigacoes"/>. Sem campo de clínica — o
/// tenant sai do token.
/// </remarks>
public sealed record ConsultaObrigacoesRequest
{
    /// <summary>Página, a partir de 1. Padrão 1.</summary>
    /// <example>1</example>
    public int? Pagina { get; init; }

    /// <summary>Itens por página, de 1 a 100. Padrão 20.</summary>
    /// <example>20</example>
    public int? Tamanho { get; init; }

    /// <summary>
    /// Estado da obrigação: PREVISTA, NOTIFICADA, RESPONDIDA, AGENDADA,
    /// CUMPRIDA, PERDIDA ou CANCELADA. Sem diferenciar maiúsculas.
    /// </summary>
    /// <example>CUMPRIDA</example>
    public string? Status { get; init; }

    /// <summary>Braço do experimento: <c>tratamento</c> ou <c>controle</c>.</summary>
    /// <example>tratamento</example>
    public string? Grupo { get; init; }

    /// <summary>Categoria do protocolo, como o core a grava (VACINA, VERMIFUGO, EXAME...).</summary>
    /// <example>VACINA</example>
    public string? Categoria { get; init; }

    /// <summary>Data prevista a partir de (inclusive), no formato AAAA-MM-DD.</summary>
    /// <example>2026-01-01</example>
    public DateOnly? De { get; init; }

    /// <summary>Data prevista até (inclusive), no formato AAAA-MM-DD.</summary>
    /// <example>2026-06-30</example>
    public DateOnly? Ate { get; init; }

    /// <summary>Id do pet.</summary>
    public long? Pet { get; init; }

    /// <summary>
    /// Campo de ordenação: <c>dataPrevista</c> (padrão), <c>status</c> (ordem do
    /// funil), <c>pet</c> (nome do pet) ou <c>valorEstimado</c>.
    /// </summary>
    /// <example>dataPrevista</example>
    public string? OrdenarPor { get; init; }

    /// <summary>Direção: <c>asc</c> (padrão) ou <c>desc</c>.</summary>
    /// <example>asc</example>
    public string? Direcao { get; init; }
}
