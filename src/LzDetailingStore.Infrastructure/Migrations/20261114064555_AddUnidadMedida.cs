using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LzDetailingStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "unidad_medida",
                columns: table => new
                {
                    clave = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidad_medida", x => x.clave);
                });

            // Fixed catalog: seeded here (not via HasData) because it is not administered from the app.
            migrationBuilder.InsertData(
                table: "unidad_medida",
                columns: new[] { "clave", "nombre", "orden" },
                values: new object[,]
                {
                    { "ml", "Mililitros", 1 },
                    { "L", "Litros", 2 },
                    { "g", "Gramos", 3 },
                    { "kg", "Kilogramos", 4 },
                    { "pza", "Pieza", 5 },
                    { "kit", "Kit", 6 }
                });

            // Normalize the legacy spelling so every existing variant satisfies the new foreign key.
            migrationBuilder.Sql("UPDATE product_variant SET size_unit = 'L' WHERE size_unit = 'litros';");

            migrationBuilder.CreateIndex(
                name: "ix_product_variant_size_unit",
                table: "product_variant",
                column: "size_unit");

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_unidad_medida_size_unit",
                table: "product_variant",
                column: "size_unit",
                principalTable: "unidad_medida",
                principalColumn: "clave",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_unidad_medida_size_unit",
                table: "product_variant");

            migrationBuilder.DropIndex(
                name: "ix_product_variant_size_unit",
                table: "product_variant");

            // 'litros' values normalized to 'L' in Up are intentionally not restored.
            migrationBuilder.DropTable(
                name: "unidad_medida");
        }
    }
}
