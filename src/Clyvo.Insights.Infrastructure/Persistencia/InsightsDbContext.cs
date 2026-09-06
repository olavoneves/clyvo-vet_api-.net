using Clyvo.Insights.Infrastructure.Persistencia.Leitura;
using Microsoft.EntityFrameworkCore;

namespace Clyvo.Insights.Infrastructure.Persistencia;

/// <summary>
/// Duas naturezas convivendo no mesmo contexto.
/// </summary>
/// <remarks>
/// <para>
/// <b>Leitura</b>: entidades sem chave mapeadas sobre as views que o core
/// publica. Não aparecem em migration.
/// </para>
/// <para>
/// <b>Escrita</b>: as tabelas que este serviço é dono, todas prefixadas
/// <c>INS_</c>. O prefixo não é decorativo — o schema Oracle da FIAP é
/// compartilhado com as tabelas do core, e é ele que evita colisão.
/// </para>
/// </remarks>
public class InsightsDbContext : DbContext
{
    public InsightsDbContext(DbContextOptions<InsightsDbContext> options) : base(options)
    {
    }

    /// <summary>Coortes por clínica e grupo, vindas de <c>VW_CLV_PAINEL_COORTE</c>.</summary>
    public DbSet<PainelCoorteView> PainelCoorte => Set<PainelCoorteView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InsightsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
