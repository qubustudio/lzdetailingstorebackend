using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LzDetailingStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropProductEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "embedding",
                table: "product");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float[]>(
                name: "embedding",
                table: "product",
                type: "real[]",
                nullable: true);
        }
    }
}
