using Clyvo.Insights.Domain.Projecoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clyvo.Insights.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// <c>INS_SNAPSHOT_COORTE</c> — o cabeçalho numérico da série de apurações.
/// </summary>
/// <remarks>
/// Só os números fechados moram aqui, e é o que permite juntá-los com o resto
/// do schema numa query. O documento completo, cuja forma varia com as metas da
/// clínica, fica no Mongo.
/// </remarks>
internal sealed class SnapshotCoorteConfiguration : IEntityTypeConfiguration<SnapshotCoorte>
{
    public void Configure(EntityTypeBuilder<SnapshotCoorte> builder)
    {
        builder.ToTable("INS_SNAPSHOT_COORTE");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("ID_SNAPSHOT_COORTE")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.IdClinica)
            .HasColumnName("ID_CLINICA")
            .IsRequired();

        // DateOnly não tem mapeamento nativo garantido no provider Oracle.
        // O converter para DATE evita depender disso e deixa a coluna legível
        // por qualquer cliente SQL.
        builder.Property(s => s.DataReferencia)
            .HasColumnName("DT_REFERENCIA")
            .HasConversion(
                data => data.ToDateTime(TimeOnly.MinValue),
                valor => DateOnly.FromDateTime(valor))
            .HasColumnType("DATE")
            .IsRequired();

        builder.Property(s => s.DeltaPontosPercentuais)
            .HasColumnName("NR_DELTA_PP")
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(s => s.ConsultasAtribuiveis)
            .HasColumnName("NR_CONSULTAS_ATRIBUIVEIS")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(s => s.ReceitaRecuperada)
            .HasColumnName("VL_RECEITA_RECUPERADA")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(s => s.TicketMedio)
            .HasColumnName("VL_TICKET_MEDIO")
            .HasPrecision(12, 2);

        builder.Property(s => s.ObrigacoesResolvidasTratado)
            .HasColumnName("QT_RESOLVIDAS_TRATADO")
            .IsRequired();

        builder.Property(s => s.ObrigacoesResolvidasControle)
            .HasColumnName("QT_RESOLVIDAS_CONTROLE")
            .IsRequired();

        // Um ponto por clínica por dia. A regra também está no repositório, que
        // procura o do dia antes de inserir; o índice é a rede embaixo.
        builder.HasIndex(s => new { s.IdClinica, s.DataReferencia })
            .IsUnique()
            .HasDatabaseName("UK_INS_SNAPSHOT_CLIN_DATA");
    }
}
