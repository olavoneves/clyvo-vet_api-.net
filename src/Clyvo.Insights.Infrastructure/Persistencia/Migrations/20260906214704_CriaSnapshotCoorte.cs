using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clyvo.Insights.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class CriaSnapshotCoorte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INS_SNAPSHOT_COORTE",
                columns: table => new
                {
                    ID_SNAPSHOT_COORTE = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_CLINICA = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    DT_REFERENCIA = table.Column<DateTime>(type: "DATE", nullable: false),
                    NR_DELTA_PP = table.Column<decimal>(type: "DECIMAL(7,2)", precision: 7, scale: 2, nullable: false),
                    NR_CONSULTAS_ATRIBUIVEIS = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: false),
                    VL_RECEITA_RECUPERADA = table.Column<decimal>(type: "DECIMAL(14,2)", precision: 14, scale: 2, nullable: false),
                    VL_TICKET_MEDIO = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    QT_RESOLVIDAS_TRATADO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    QT_RESOLVIDAS_CONTROLE = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INS_SNAPSHOT_COORTE", x => x.ID_SNAPSHOT_COORTE);
                });

            migrationBuilder.CreateIndex(
                name: "UK_INS_SNAPSHOT_CLIN_DATA",
                table: "INS_SNAPSHOT_COORTE",
                columns: new[] { "ID_CLINICA", "DT_REFERENCIA" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INS_SNAPSHOT_COORTE");
        }
    }
}
