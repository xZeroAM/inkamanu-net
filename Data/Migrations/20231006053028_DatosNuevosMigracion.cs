using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace proyecto_inkamanu_net.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatosNuevosMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_proforma_Producto_Productoid",
                table: "t_proforma");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.RenameColumn(
                name: "Apellidos",
                table: "AspNetUsers",
                newName: "Genero");

            migrationBuilder.AddColumn<string>(
                name: "ApellidoMat",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApellidoPat",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Celular",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "fechaDeActualizacion",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fechaDeNacimiento",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "fechaDeRegistro",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "t_producto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Imagen = table.Column<string>(type: "text", nullable: false),
                    Precio = table.Column<double>(type: "double precision", nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    GraduacionAlcoholica = table.Column<double>(type: "double precision", nullable: true),
                    TipoCerveza = table.Column<string>(type: "text", nullable: false),
                    Volumen = table.Column<double>(type: "double precision", nullable: true),
                    TipoEnvase = table.Column<string>(type: "text", nullable: false),
                    fechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_producto", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_t_proforma_t_producto_Productoid",
                table: "t_proforma",
                column: "Productoid",
                principalTable: "t_producto",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_proforma_t_producto_Productoid",
                table: "t_proforma");

            migrationBuilder.DropTable(
                name: "t_producto");

            migrationBuilder.DropColumn(
                name: "ApellidoMat",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ApellidoPat",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Celular",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "fechaDeActualizacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "fechaDeNacimiento",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "fechaDeRegistro",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Genero",
                table: "AspNetUsers",
                newName: "Apellidos");

            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Imagen = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Precio = table.Column<double>(type: "double precision", nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    fechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_t_proforma_Producto_Productoid",
                table: "t_proforma",
                column: "Productoid",
                principalTable: "Producto",
                principalColumn: "id");
        }
    }
}
