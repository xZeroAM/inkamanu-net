using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proyecto_inkamanu_net.Data.Migrations
{
    /// <inheritdoc />
    public partial class AtributosNuevosPedidoMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Descuento",
                table: "t_order",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Regalo",
                table: "t_order",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "t_order");

            migrationBuilder.DropColumn(
                name: "Regalo",
                table: "t_order");
        }
    }
}
