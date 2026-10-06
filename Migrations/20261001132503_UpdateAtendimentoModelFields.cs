using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAtendimentoModelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataAtendimento",
                table: "Atendimentos");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFinalizacaoAtendimento",
                table: "Atendimentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInicioAtendimento",
                table: "Atendimentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacao",
                table: "Atendimentos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataFinalizacaoAtendimento",
                table: "Atendimentos");

            migrationBuilder.DropColumn(
                name: "DataInicioAtendimento",
                table: "Atendimentos");

            migrationBuilder.DropColumn(
                name: "Observacao",
                table: "Atendimentos");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAtendimento",
                table: "Atendimentos",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
