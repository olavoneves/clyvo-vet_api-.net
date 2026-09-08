using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clyvo.Insights.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class CriaMetaIndicador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INS_META_INDICADOR",
                columns: table => new
                {
                    ID_META_INDICADOR = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_CLINICA = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    DS_INDICADOR = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: false),
                    NR_LIMIAR_PERCENTUAL = table.Column<decimal>(type: "DECIMAL(5,2)", precision: 5, scale: 2, nullable: false),
                    DT_CRIACAO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DT_ATUALIZACAO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INS_META_INDICADOR", x => x.ID_META_INDICADOR);
                });

            migrationBuilder.CreateIndex(
                name: "UK_INS_META_CLINICA_INDIC",
                table: "INS_META_INDICADOR",
                columns: new[] { "ID_CLINICA", "DS_INDICADOR" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INS_META_INDICADOR");
        }
    }
}
