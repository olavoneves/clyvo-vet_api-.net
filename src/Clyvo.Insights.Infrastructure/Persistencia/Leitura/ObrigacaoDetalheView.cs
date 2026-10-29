namespace Clyvo.Insights.Infrastructure.Persistencia.Leitura;

/// <summary>
/// Uma linha de <c>VW_CLV_OBRIGACAO_DETALHE</c> — uma por obrigação, já com
/// pet, tutor, protocolo e etapa por nome.
/// </summary>
/// <remarks>
/// <para>
/// A terceira view que o core publica, criada pela migration V16 dele para esta
/// listagem. O contrato de colunas é fixo: renomear ou acrescentar coluna do
/// lado de lá é mudança de contrato, não detalhe de implementação.
/// </para>
/// <para>
/// Sem chave declarada, como as outras duas, e pelo mesmo motivo: fica fora das
/// migrations. <c>ID_OBRIGACAO</c> é único na prática, mas declarar chave numa
/// view de outro serviço faria o EF tratar como erro dele uma duplicata que só
/// o core poderia produzir.
/// </para>
/// </remarks>
public sealed class ObrigacaoDetalheView
{
    public long IdObrigacao { get; init; }

    /// <summary>Recorte de tenant. A view não filtra: quem recorta é quem consome.</summary>
    public long IdClinica { get; init; }

    public long IdPet { get; init; }

    public string NomePet { get; init; } = string.Empty;

    /// <summary>Só o nome. A view não publica e-mail nem telefone do tutor.</summary>
    public string NomeTutor { get; init; } = string.Empty;

    public string Protocolo { get; init; } = string.Empty;

    public string Categoria { get; init; } = string.Empty;

    public string Etapa { get; init; } = string.Empty;

    /// <summary>Código do status, como o core grava: PREVISTA, NOTIFICADA...</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>'S' controle, 'N' tratado. A flag persistida, nunca o sorteio recalculado.</summary>
    public string GrupoControle { get; init; } = string.Empty;

    public DateTime DataPrevista { get; init; }

    public DateTime JanelaInicio { get; init; }

    public DateTime JanelaFim { get; init; }

    public decimal? ValorEstimado { get; init; }

    public decimal? ValorRealizado { get; init; }
}
