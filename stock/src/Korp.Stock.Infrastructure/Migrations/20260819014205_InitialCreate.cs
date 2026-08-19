using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stock");

            migrationBuilder.CreateTable(
                name: "entity_references",
                schema: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entity_references", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    product_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stocks",
                schema: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocks", x => x.id);
                    table.CheckConstraint("ck_stocks_quantity_non_negative", "quantity >= 0");
                    table.ForeignKey(
                        name: "fk_stocks_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "stock",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                schema: "stock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.CheckConstraint("ck_transactions_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_transactions_entity_references_reference_id",
                        column: x => x.reference_id,
                        principalSchema: "stock",
                        principalTable: "entity_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transactions_stocks_stock_id",
                        column: x => x.stock_id,
                        principalSchema: "stock",
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_products_product_code",
                schema: "stock",
                table: "products",
                column: "product_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stocks_product_id",
                schema: "stock",
                table: "stocks",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_reference_id",
                schema: "stock",
                table: "transactions",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_stock_id_created_at",
                schema: "stock",
                table: "transactions",
                columns: new[] { "stock_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactions",
                schema: "stock");

            migrationBuilder.DropTable(
                name: "entity_references",
                schema: "stock");

            migrationBuilder.DropTable(
                name: "stocks",
                schema: "stock");

            migrationBuilder.DropTable(
                name: "products",
                schema: "stock");
        }
    }
}
