using Clyvo.Insights.Infrastructure.Persistencia.Leitura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clyvo.Insights.Infrastructure.Persistencia.Configuracoes;

internal sealed class ObrigacaoDetalheViewConfiguration : IEntityTypeConfiguration<ObrigacaoDetalheView>
{
    public void Configure(EntityTypeBuilder<ObrigacaoDetalheView> builder)
    {
        builder.HasNoKey().ToView("VW_CLV_OBRIGACAO_DETALHE");

        builder.Property(v => v.IdObrigacao).HasColumnName("ID_OBRIGACAO");
        builder.Property(v => v.IdClinica).HasColumnName("ID_CLINICA");
        builder.Property(v => v.IdPet).HasColumnName("ID_PET");
        builder.Property(v => v.NomePet).HasColumnName("NM_PET");
        builder.Property(v => v.NomeTutor).HasColumnName("NM_TUTOR");
        builder.Property(v => v.Protocolo).HasColumnName("NM_PROTOCOLO");
        builder.Property(v => v.Categoria).HasColumnName("DS_CATEGORIA");
        builder.Property(v => v.Etapa).HasColumnName("NM_ETAPA");
        builder.Property(v => v.Status).HasColumnName("DS_STATUS");
        builder.Property(v => v.GrupoControle).HasColumnName("FL_GRUPO_CONTROLE");
        // DATE explícito: o período filtra por DT_PREVISTA, e o parâmetro deve
        // chegar com o mesmo tipo da coluna em vez de TIMESTAMP, que obrigaria
        // o Oracle a converter a coluna linha a linha.
        builder.Property(v => v.DataPrevista).HasColumnName("DT_PREVISTA").HasColumnType("DATE");
        builder.Property(v => v.JanelaInicio).HasColumnName("DT_JANELA_INICIO").HasColumnType("DATE");
        builder.Property(v => v.JanelaFim).HasColumnName("DT_JANELA_FIM").HasColumnType("DATE");
        builder.Property(v => v.ValorEstimado).HasColumnName("NR_VALOR_ESTIMADO").HasPrecision(10, 2);
        builder.Property(v => v.ValorRealizado).HasColumnName("NR_VALOR_REALIZADO").HasPrecision(10, 2);
    }
}
