using System.Linq.Expressions;
using Clyvo.Insights.Domain.Metas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clyvo.Insights.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// <c>INS_META_INDICADOR</c> — tabela que este serviço é dono e governa por
/// migration.
/// </summary>
/// <remarks>
/// O prefixo <c>INS_</c> não é decorativo. O schema Oracle da FIAP é
/// compartilhado com as tabelas <c>TB_CLV_</c> do core, e sem o prefixo a
/// primeira migration deste serviço colidiria com o schema de produção do
/// outro.
/// </remarks>
internal sealed class MetaIndicadorConfiguration : IEntityTypeConfiguration<MetaIndicador>
{
    public void Configure(EntityTypeBuilder<MetaIndicador> builder)
    {
        builder.ToTable("INS_META_INDICADOR");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("ID_META_INDICADOR")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.IdClinica)
            .HasColumnName("ID_CLINICA")
            .IsRequired();

        // Guardado como texto: o número do enum num banco compartilhado obriga
        // quem olha a tabela a ter o código em mãos para saber o que é 1.
        builder.Property(m => m.Indicador)
            .HasColumnName("DS_INDICADOR")
            .HasMaxLength(40)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.LimiarPercentual)
            .HasColumnName("NR_LIMIAR_PERCENTUAL")
            .HasPrecision(5, 2)
            .IsRequired();

        // Oracle guarda TIMESTAMP sem fuso, e o EF devolve DateTimeKind.Unspecified.
        // Sem o converter, a mesma propriedade sai serializada com Z quando vem
        // da memória e sem Z quando vem do banco — o cliente recebe duas formas
        // do mesmo campo dependendo de a meta ter acabado de ser criada.
        builder.Property(m => m.DataCriacao)
            .HasColumnName("DT_CRIACAO")
            .HasConversion(ParaBanco, ParaUtc)
            .IsRequired();

        builder.Property(m => m.DataAtualizacao)
            .HasColumnName("DT_ATUALIZACAO")
            .HasConversion(ParaBanco, ParaUtc)
            .IsRequired();

        // Um piso por indicador por clínica. A regra também está no caso de uso,
        // que devolve 409 antes de tentar gravar; o índice é a rede embaixo,
        // para o caso de duas requisições concorrentes passarem pela checagem.
        builder.HasIndex(m => new { m.IdClinica, m.Indicador })
            .IsUnique()
            .HasDatabaseName("UK_INS_META_CLINICA_INDIC");
    }

    private static readonly Expression<Func<DateTime, DateTime>> ParaBanco =
        valor => valor;

    private static readonly Expression<Func<DateTime, DateTime>> ParaUtc =
        valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc);
}
