namespace Clyvo.Insights.Infrastructure.Persistencia.Leitura;

/// <summary>
/// Uma linha de <c>VW_CLV_PAINEL_RECEITA</c> — funil mensal de obrigações por
/// clínica e grupo.
/// </summary>
/// <remarks>
/// <para>
/// A segunda view publicada pelo core, e a única com a distribuição por estado.
/// As colunas do funil são <b>cumulativas</b>: <c>QT_NOTIFICADAS</c> conta tudo
/// que chegou pelo menos até NOTIFICADA. Somá-las daria mais que o total, e a
/// conversão para baldes exclusivos é feita no domínio.
/// </para>
/// <para>
/// <c>PC_COMPARECIMENTO</c> não é mapeada: a view a calcula por mês, e média de
/// percentual mensal não é o percentual da janela.
/// </para>
/// </remarks>
public sealed class PainelReceitaView
{
    public long IdClinica { get; init; }

    /// <summary>Primeiro dia do mês de vencimento das obrigações agrupadas.</summary>
    public DateTime MesReferencia { get; init; }

    /// <summary>'S' controle, 'N' tratado.</summary>
    public string GrupoControle { get; init; } = string.Empty;

    public int Total { get; init; }

    public int AlcancaramNotificada { get; init; }

    public int AlcancaramRespondida { get; init; }

    public int AlcancaramAgendada { get; init; }

    public int Cumpridas { get; init; }

    public int Perdidas { get; init; }

    public decimal ValorRecuperado { get; init; }

    public decimal ValorPerdido { get; init; }
}
