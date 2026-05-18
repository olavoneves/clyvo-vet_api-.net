using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.net.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TUTORES",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TELEFONE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CPF = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TUTORES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PETS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ESPECIE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    RACA = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DATA_NASCIMENTO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    TUTOR_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PETS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PETS_TUTORES_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TUTORES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CONSULTAS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    DATA = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DESCRICAO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DIAGNOSTICO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    VETERINARIO_NOME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PET_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONSULTAS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CONSULTAS_PETS_PET_ID",
                        column: x => x.PET_ID,
                        principalTable: "PETS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTAS_PET_ID",
                table: "CONSULTAS",
                column: "PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PETS_TUTOR_ID",
                table: "PETS",
                column: "TUTOR_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONSULTAS");

            migrationBuilder.DropTable(
                name: "PETS");

            migrationBuilder.DropTable(
                name: "TUTORES");
        }
    }
}
