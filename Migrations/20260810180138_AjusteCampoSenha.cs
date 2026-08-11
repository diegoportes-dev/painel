using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud.Migrations
{
    /// <inheritdoc />
    public partial class AjusteCampoSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "Usuarios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "Usuarios",
                type: "TEXT",
                nullable: true);
        }
    }
}
