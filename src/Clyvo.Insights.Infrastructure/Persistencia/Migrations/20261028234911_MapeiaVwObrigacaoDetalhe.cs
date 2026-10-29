using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clyvo.Insights.Infrastructure.Persistencia.Migrations
{
    /// <summary>
    /// Registra <c>VW_CLV_OBRIGACAO_DETALHE</c> no modelo, sem DDL nenhum.
    /// </summary>
    /// <remarks>
    /// A view é do core (V16 dele) e entra como entidade sem chave com
    /// <c>ToView</c>, que o EF não materializa em migration — por isso Up e Down
    /// vazios. A migration existe para o snapshot acompanhar o modelo: sem ela, a
    /// próxima migration de verdade arrastaria esta mudança junto, e o histórico
    /// em <c>INS_EF_MIGRATIONS</c> deixaria de dizer quando o contrato de leitura
    /// passou a incluir a terceira view.
    /// </remarks>
    public partial class MapeiaVwObrigacaoDetalhe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nada a criar: a view é do core.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nada a desfazer: a view continua sendo do core.
        }
    }
}
