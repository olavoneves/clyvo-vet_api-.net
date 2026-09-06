namespace Clyvo.Insights.Infrastructure.Persistencia.Leitura;

/// <summary>
/// Uma linha de <c>VW_CLV_PAINEL_COORTE</c>, a view que o clyvo-core publica
/// como contrato de integração.
/// </summary>
/// <remarks>
/// <para>
/// Entidade sem chave: a view agrega por clínica e grupo e não tem identidade
/// própria. O EF ignora entidades mapeadas com <c>ToView</c> ao gerar migration,
/// que é exatamente o que se quer — migrations governam só o que este serviço é
/// dono, e o resto é leitura sobre contrato publicado.
/// </para>
/// <para>
/// <b>PC_CUMPRIMENTO não é mapeada de propósito.</b> A view calcula o
/// percentual, mas quem o deriva aqui é o domínio, a partir do numerador e do
/// denominador. Trazer o percentual pronto criaria dois caminhos para o mesmo
/// número, e eles divergem no dia em que um dos lados mudar o arredondamento.
/// </para>
/// </remarks>
public sealed class PainelCoorteView
{
    /// <summary>Clínica dona das obrigações. Recorte de tenant.</summary>
    public long IdClinica { get; init; }

    /// <summary>TRATADO ou CONTROLE, já resolvido pela view a partir da flag persistida.</summary>
    public string Grupo { get; init; } = string.Empty;

    /// <summary>
    /// Obrigações do grupo com desfecho. A view filtra
    /// <c>ds_status IN ('CUMPRIDA','PERDIDA')</c>, então esta contagem já é o
    /// denominador certo — obrigação que ainda não venceu não entra.
    /// </summary>
    public int ObrigacoesResolvidas { get; init; }

    /// <summary>Dessas, quantas terminaram em CUMPRIDA.</summary>
    public int ObrigacoesCumpridas { get; init; }

    /// <summary>Pets distintos no grupo. Diz se o delta se sustenta.</summary>
    public int PetsDistintos { get; init; }

    /// <summary>
    /// Ticket médio apurado das consultas realizadas da clínica — o que ela de
    /// fato cobrou, não o valor declarado no cadastro. Nulo quando não há
    /// consulta valorada.
    /// </summary>
    public decimal? TicketMedio { get; init; }
}
