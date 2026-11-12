using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LzDetailingStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_relation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_relation", x => x.id);
                    table.CheckConstraint("ck_product_relation_not_self", "product_id <> related_product_id");
                    table.ForeignKey(
                        name: "fk_product_relation_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_relation_product_related_product_id",
                        column: x => x.related_product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_relation_product_id_related_product_id_type",
                table: "product_relation",
                columns: new[] { "product_id", "related_product_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_relation_product_id_status_type_sort_order",
                table: "product_relation",
                columns: new[] { "product_id", "status", "type", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_product_relation_related_product_id",
                table: "product_relation",
                column: "related_product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_relation");
        }
    }
}
