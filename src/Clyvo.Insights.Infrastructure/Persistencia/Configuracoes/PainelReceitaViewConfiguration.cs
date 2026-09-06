using Clyvo.Insights.Infrastructure.Persistencia.Leitura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clyvo.Insights.Infrastructure.Persistencia.Configuracoes;

internal sealed class PainelReceitaViewConfiguration : IEntityTypeConfiguration<PainelReceitaView>
{
    public void Configure(EntityTypeBuilder<PainelReceitaView> builder)
    {
        builder.HasNoKey().ToView("VW_CLV_PAINEL_RECEITA");

        builder.Property(v => v.IdClinica).HasColumnName("ID_CLINICA");
        builder.Property(v => v.MesReferencia).HasColumnName("MES_REFERENCIA");
        builder.Property(v => v.GrupoControle).HasColumnName("FL_GRUPO_CONTROLE");
        builder.Property(v => v.Total).HasColumnName("QT_OBRIGACOES");
        builder.Property(v => v.AlcancaramNotificada).HasColumnName("QT_NOTIFICADAS");
        builder.Property(v => v.AlcancaramRespondida).HasColumnName("QT_RESPONDIDAS");
        builder.Property(v => v.AlcancaramAgendada).HasColumnName("QT_AGENDADAS");
        builder.Property(v => v.Cumpridas).HasColumnName("QT_CUMPRIDAS");
        builder.Property(v => v.Perdidas).HasColumnName("QT_PERDIDAS");
        builder.Property(v => v.ValorRecuperado).HasColumnName("VL_RECUPERADO").HasPrecision(14, 2);
        builder.Property(v => v.ValorPerdido).HasColumnName("VL_PERDIDO").HasPrecision(14, 2);
    }
}
