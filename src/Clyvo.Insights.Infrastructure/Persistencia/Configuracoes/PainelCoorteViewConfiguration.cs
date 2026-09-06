using Clyvo.Insights.Infrastructure.Persistencia.Leitura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clyvo.Insights.Infrastructure.Persistencia.Configuracoes;

internal sealed class PainelCoorteViewConfiguration : IEntityTypeConfiguration<PainelCoorteView>
{
    public void Configure(EntityTypeBuilder<PainelCoorteView> builder)
    {
        builder.HasNoKey().ToView("VW_CLV_PAINEL_COORTE");

        builder.Property(v => v.IdClinica).HasColumnName("ID_CLINICA");
        builder.Property(v => v.Grupo).HasColumnName("DS_GRUPO");
        builder.Property(v => v.ObrigacoesResolvidas).HasColumnName("QT_OBRIGACOES");
        builder.Property(v => v.ObrigacoesCumpridas).HasColumnName("QT_CUMPRIDAS");
        builder.Property(v => v.PetsDistintos).HasColumnName("QT_PETS");
        // A view devolve AVG(nr_valor) arredondado a 2 casas sobre NUMBER(10,2).
        // Sem precisão explícita o EF avisa que pode truncar em silêncio.
        builder.Property(v => v.TicketMedio)
            .HasColumnName("VL_TICKET_MEDIO")
            .HasPrecision(12, 2);
    }
}
