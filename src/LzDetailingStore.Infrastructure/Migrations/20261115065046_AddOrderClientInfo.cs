using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LzDetailingStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderClientInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_ip",
                table: "order",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_user_agent",
                table: "order",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_ip",
                table: "order");

            migrationBuilder.DropColumn(
                name: "client_user_agent",
                table: "order");
        }
    }
}
